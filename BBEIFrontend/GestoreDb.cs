using Dapper;
using BBEIDataAccess;
using BBEIDataAccess.Models;
using BBEIFrontend;
using BBEILib;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Claims;
using System.Xml.Serialization;

namespace BBEIFrontend
{
    public class GestoreDb
    {
        private UserManager<ApplicationUser> userManager;
        private RoleManager<IdentityRole> roleManager;
        private BBEIDataAccess.IDataBase db;
        private BBEIContext ef_db;
        public DateTime LastReadCounters = DateTime.Now;
        public DateTime LastReadTransits = DateTime.Now;
        public DateTime LastRefreshContainersGui = DateTime.Now;


        static List<int> TransientErrorNumbers = new() { 4060, 40197, 40501, 40613, 49918, 49919, 49920, 11001 };

        public GestoreDb()
        {
            var optionsBuilder = new DbContextOptionsBuilder<BBEIContext>();
            optionsBuilder.UseSqlite(Program.CoreIdentity.ConnectionString);
            db = new DataBaseFactory().CreateDatabase("BBEIDataAccess.SqliteDataBase", Program.CoreIdentity.ConnectionString);
            ef_db = new BBEIContext(optionsBuilder.Options);
            if (!Program.SystemUsersCreated || !Program.SystemRolesCreated || !Program.SystemUserRolesCreated)
                CreateSystemIdentities();
        }



        private void CreateSystemIdentities()
        {
            userManager = Program.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            roleManager = Program.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            ManageUsers manUsers = new ManageUsers(userManager, roleManager);

            if (!Program.SystemUsersCreated)
            {
                try
                {
                    bool adminCreated = false;

                    if (!string.IsNullOrEmpty(Program.CoreIdentity.BBEIAdminUser) && !manUsers.ExistsUser(Program.CoreIdentity.BBEIAdminUser) && !string.IsNullOrEmpty(Program.CoreIdentity.BBEIAdminPassword))
                    {
                        if (!manUsers.CreateUser(Program.CoreIdentity.BBEIAdminUser, "b4mb1n@hotmail.it", Program.CoreIdentity.BBEIAdminPassword, "", "", "", false, out string message))
                            Log.Error("StargateReleaseManagerAdmin not created: " + message, null);
                        else
                            adminCreated = true;
                    }
                    else if (!string.IsNullOrEmpty(Program.CoreIdentity.BBEIAdminUser) && !string.IsNullOrEmpty(Program.CoreIdentity.BBEIAdminPassword))
                        adminCreated = true;

                    if (adminCreated || string.IsNullOrEmpty(Program.CoreIdentity.BBEIAdminUser))
                        Program.SystemUsersCreated = true;
                }
                catch (Exception userEx)
                {
                    Log.Error("Create default user error: " + userEx.Message);
                }
            }

            if (!Program.SystemRolesCreated)
            {
                try
                {
                    bool adminCreated = false;
                    bool mantainerCreated = false;
                    bool userCreated = false;

                    if (!manUsers.ExistsRole("ADMIN"))
                    {
                        if (!manUsers.CreateRole("ADMIN", out string message))
                            Log.Error("Role ADMIN not created: " + message, null);
                        else
                            adminCreated = true;
                    }
                    else
                        adminCreated = true;

                    if (!manUsers.ExistsRole("MANTAINER"))
                    {
                        if (!manUsers.CreateRole("MANTAINER", out string message))
                            Log.Error("Role MANTAINER not created: " + message, null);
                        else
                            mantainerCreated = true;
                    }
                    else
                        mantainerCreated = true;

                    if (!manUsers.ExistsRole("USER"))
                    {
                        if (!manUsers.CreateRole("USER", out string message))
                            Log.Error("Role USER not created: " + message, null);
                        else
                            userCreated = true;
                    }
                    else
                        userCreated = true;

                    if (adminCreated && mantainerCreated && userCreated)
                        Program.SystemRolesCreated = true;
                }
                catch (Exception userEx)
                {
                    Log.Error("Create default user error: " + userEx.Message);
                }
            }

            if (!Program.SystemUserRolesCreated)
            {
                try
                {
                    bool adminAssigned = false;

                    if (manUsers.ExistsUser(Program.CoreIdentity.BBEIAdminUser) && manUsers.ExistsRole("ADMIN") && !manUsers.ExistsUserRole(Program.CoreIdentity.BBEIAdminUser, "ADMIN"))
                    {
                        if (!manUsers.CreateUserRole(Program.CoreIdentity.BBEIAdminUser, "ADMIN", out string message))
                            Log.Error("UserRole for ADMIN not assigned: " + message, null);
                        else
                            adminAssigned = true;
                    }
                    else if (manUsers.ExistsUserRole(Program.CoreIdentity.BBEIAdminUser, "ADMIN"))
                        adminAssigned = true;

                    if (adminAssigned || string.IsNullOrEmpty(Program.CoreIdentity.BBEIAdminUser))
                        Program.SystemUserRolesCreated = true;
                }

                catch (Exception userEx)
                {
                    Log.Error("Create default user error: " + userEx.Message);
                }

            }
        }

        public bool SRMUserCreateGeneric(string userEmail, string userPassword, string userRole, string userSecEmail, string userPhoneNumber, string userFullName)
        {
            bool bRet = false;
            try
            {
                userManager = Program.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                roleManager = Program.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                ManageUsers manUsers = new ManageUsers(userManager, roleManager);

                if (Program.SystemUsersCreated)
                {
                    //Creazione dell'utente:
                    if (!string.IsNullOrEmpty(userEmail) && !manUsers.ExistsUser(userEmail) && !string.IsNullOrEmpty(userPassword))
                    {
                        //Se l'utente non esiste già provo a crearlo
                        if (!manUsers.CreateUser(userEmail, userEmail, userPassword, userSecEmail, userPhoneNumber, userFullName, true, out string message))
                        {
                            Log.Error("StargateReleaseManager generic user " + userEmail + " not created: " + message, null);
                        }
                        else
                        {
                            //La creazione dell'utente è andata a buon fine, procedo con la creazione della entry nella tabella del suo ruolo
                            //Mi tengo da parte l'Id dell'utente appena inserito
                            string userId = SRMUserAspNetGetIdByName(userEmail);
                            if (!string.IsNullOrEmpty(userId))
                            {
                                if (manUsers.ExistsUser(userEmail) && !manUsers.ExistsUserRole(userEmail, userRole))
                                {
                                    if (!manUsers.CreateUserRole(userEmail, userRole, out string messageRole))
                                    {
                                        Log.Error("UserRole for generic user " + userEmail + " not assigned: " + messageRole, null);
                                    }
                                    else
                                        bRet = true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                bRet = false;
                Log.Error("SRMUserCreateGeneric error: " + ex.Message);
            }
            return bRet;
        }

        public bool SRMUserAspNetUpdateRole(string userName, string newRoleName)
        {
            bool bRet = false;
            try
            {
                string idUser = SRMUserAspNetGetIdByName(userName);
                string idRole = SRMUserAspNetGetRoleIdByName(newRoleName);

                if (!string.IsNullOrEmpty(idUser) && !string.IsNullOrEmpty(idRole))
                {
                    DbManager dbManager = new DbManager(Program.CoreIdentity.ConnectionString);
                    bRet = dbManager.UpdateUserRole(idUser, idRole);
                }
                else
                {
                    Log.Error("SRMUserAspNetUpdateRole error: Unable to get userId or roleId");
                }

            }
            catch (Exception ex)
            {
                bRet = false;
                Log.Error("SRMUserAspNetUpdateRole error: " + ex.Message);
            }
            return bRet;
        }


        public string SRMUserAspNetGetIdByName(string userName)
        {
            string oStr = "";
            try
            {
                DbManager dbManager = new DbManager(Program.CoreIdentity.ConnectionString);
                oStr = dbManager.GetUserId(userName);

            }
            catch (Exception ex)
            {
                oStr = "";
                Log.Error("SRMUserAspNetGetIdByName error: " + ex.Message);
            }
            return oStr;
        }

        public string SRMUserAspNetGetRoleIdByName(string roleName)
        {
            string oStr = "";
            try
            {
                DbManager dbManager = new DbManager(Program.CoreIdentity.ConnectionString);
                oStr = dbManager.GetRoleId(roleName);

            }
            catch (Exception ex)
            {
                oStr = "";
                Log.Error("SRMUserAspNetGetRoleIdByName error: " + ex.Message);
            }
            return oStr;
        }

        public bool SRMUserDelete(UserManager<ApplicationUser> mUserManager, string userName)
        {
            bool bRet = false;
            try
            {
                if (Program.SystemUsersCreated)
                {
                    DbManager dbManager = new DbManager(Program.CoreIdentity.ConnectionString);
                    //Verifico l'esistenza dell'utente
                    Task<ApplicationUser> findTask = Task.Run(() => mUserManager.FindByNameAsync(userName));
                    Task.WaitAll(findTask);
                    if (findTask.Result != null)
                    {

                        ApplicationUser user = findTask.Result;

                        string userId = SRMUserAspNetGetIdByName(userName);

                        //Tolgo di mezzo l'utente da identity, dovrebbe toglierla dagli users e anche dalla tabella users/roles
                        Task<IdentityResult> deleteTask = Task.Run(() => mUserManager.DeleteAsync(user));
                        Task.WaitAll(deleteTask);
                        if (deleteTask.Result.Succeeded)
                        {
                            //Utente cancellato dall'identity, ora rimane da toglierlo di mezzo nella nostra parte di db, ovvero nella mappatura tra
                            //userId e suoi customers
                            bRet = dbManager.DeleteUser(userId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                bRet = false;
                Log.Error("SRMUserDelete error: " + ex.Message);
            }
            return bRet;
        }

        public List<UserInfo> SRMGetVisibleUsersForIdentity(ClaimsPrincipal loggedInAs)
        {
            List<UserInfo> oList = new List<UserInfo>();
            try
            {
                DbManager dbManager = new DbManager(Program.CoreIdentity.ConnectionString);

                //TODO different roles
                bool imSystemUser = false;
                if (loggedInAs.Claims.Any(x => x.Value == "ADMIN"))
                    imSystemUser = true;

                List<UserInfo> totList = new();
                UserInfo mine = new();

                totList = dbManager.GetUsers(imSystemUser);

                if (totList.Count > 0)
                {
                    foreach (var dui in totList)
                    {
                        //Mi segno chi sono io
                        if (dui.UserName == loggedInAs.Identity.Name)
                        {
                            mine = dui;
                        }
                    }

                    //Passo di raffinamento: faccio uscire solo gli utenti abilitati
                    if (loggedInAs.IsInRole("ADMIN"))
                    {
                        //Amministratore, passa tutto
                        oList = totList;
                    }
                }

            }
            catch (Exception ex)
            {
                oList = new();
                Log.Error("SRMGetVisibleUsersForIdentity error: " + ex.Message);
            }
            return oList;
        }

        public UserInfo SRMGetUserFromUserId(string userId)
        {
            UserInfo o = new UserInfo();
            try
            {
                DbManager dbManager = new DbManager(Program.CoreIdentity.ConnectionString);
                o = dbManager.GetUserInfoById(userId);
            }
            catch (Exception ex)
            {
                o = new();
                Log.Error("SRMGetVisibleUsersForIdentity error: " + ex.Message);
            }
            return o;
        }

        public bool SRMUserUpdate(UserManager<ApplicationUser> mUserManager, string userName, string newNameEmail, string newPassword, string newRole, string newSecEmail, string newPhoneNumber, string newFullName)
        {
            bool bRet = false;
            try
            {
                if (Program.SystemUsersCreated)
                {
                    //Modifica di un utente esistente: verifico che nel db non sia già presente un utente con
                    //il nome utente nuovo che voglio dare al mio utente attuale, e cambio sia nome che mail
                    if (!string.IsNullOrEmpty(newNameEmail) && SRMUserAspNetGetIdByName(newNameEmail) == string.Empty)
                    {
                        //Verifico l'esistenza dell'utente attuale
                        Task<ApplicationUser> findTask = Task.Run(() => mUserManager.FindByNameAsync(userName));
                        Task.WaitAll(findTask);
                        if (findTask.Result != null)
                        {
                            ApplicationUser user = findTask.Result;
                            Task<IdentityResult> setUserNameTaks = Task.Run(() => mUserManager.SetUserNameAsync(user, newNameEmail));
                            Task.WaitAll(setUserNameTaks);
                            if (setUserNameTaks.Result.Succeeded)
                            {
                                //Cambio di nome avvenuto correttamente!
                                bRet = true;
                                //Così come ho cambiato il nome, devo cambiare la mail (per noi hanno uguale valenza
                                Task<string> resetTokenMailTask = Task.Run(() => mUserManager.GenerateChangeEmailTokenAsync(user, newNameEmail));
                                Task.WaitAll(resetTokenMailTask);
                                string token = resetTokenMailTask.Result;

                                Task<IdentityResult> resetMailTask = Task.Run(() => mUserManager.ChangeEmailAsync(user, token, newNameEmail));
                                Task.WaitAll(resetMailTask);

                                if (resetMailTask.Result.Succeeded)
                                {
                                    bRet = true;
                                    userName = newNameEmail;
                                }
                                else
                                {
                                    bRet = false;
                                }
                            }
                            else
                            {
                                bRet = false;
                            }
                        }
                    }
                    else
                    {
                        //Non dovevo cambiare nome allo user, proseguiamo
                        bRet = true;
                    }

                    //Cambio/reset password se necessario
                    if (bRet)
                    {
                        if (!string.IsNullOrEmpty(newPassword))
                        {
                            bool passChange = SRMUserChangePasswordOnFirstAccess(mUserManager, userName, newPassword);
                            if (passChange)
                            {
                                //La pass è stata cambiata correttamente ma devo abbassare il flag di email confermata, in modo
                                //da, al prossimo login, permettere all'utente di cambiare la password a suo piacimento
                                Task<ApplicationUser> findUserTaskAfterReset = Task.Run(() => mUserManager.FindByNameAsync(userName));
                                Task.WaitAll(findUserTaskAfterReset);
                                ApplicationUser userAfterReset = findUserTaskAfterReset.Result;

                                userAfterReset.EmailConfirmed = false;

                                Task<IdentityResult> updateTask = Task.Run(() => mUserManager.UpdateAsync(userAfterReset));
                                Task.WaitAll(updateTask);
                                if (updateTask.Result.Succeeded)
                                {
                                    bRet = true;
                                }
                            }
                        }
                        else
                        {
                            bRet = true;
                        }
                    }

                    //Attribuzione nuovo ruolo se necessario
                    if (bRet)
                    {
                        if (!string.IsNullOrEmpty(newRole))
                        {
                            //Devo cercare sul DB nella tabella users/roles l'Id del mio user e alterarla con il nuovo codice ruolo
                            bRet = SRMUserAspNetUpdateRole(userName, newRole);
                        }
                    }

                    //Update others fields
                    if (bRet)
                    {
                        if (!string.IsNullOrEmpty(newSecEmail) || !string.IsNullOrEmpty(newFullName))
                        {
                            //Devo cercare sul DB nella tabella users/roles l'Id del mio user e alterarla con il nuovo codice ruolo
                            bRet = SRMUserAspNetUpdateOthersFields(mUserManager, userName, newSecEmail, newPhoneNumber, newFullName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                bRet = false;
                Log.Error("SRMUserUpdate error: " + ex.Message);
            }
            return bRet;
        }

        private bool SRMUserAspNetUpdateOthersFields(UserManager<ApplicationUser> mUserManager, string userName, string newSecEmail, string newPhoneNumber, string newFullName)
        {
            bool bRet = false;
            try
            {
                if (Program.SystemUsersCreated)
                {
                    //Creazione dell'utente:
                    if (!string.IsNullOrEmpty(userName))
                    {
                        //Verifico l'esistenza dell'utente
                        Task<ApplicationUser> findTask = Task.Run(() => mUserManager.FindByNameAsync(userName));
                        Task.WaitAll(findTask);
                        if (findTask.Result != null)
                        {
                            ApplicationUser user = findTask.Result;

                            user.SecondaryEmailAddress = newSecEmail;
                            user.PhoneNumber = newPhoneNumber;
                            user.Name = newFullName;

                            Task<IdentityResult> updateTask = Task.Run(() => mUserManager.UpdateAsync(user));
                            Task.WaitAll(updateTask);
                            if (updateTask.Result.Succeeded)
                            {
                                bRet = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                bRet = false;
                Log.Error("SRMUserChangePasswordOnFirstAccess error: " + ex.Message);
            }
            return bRet;
        }

        public bool SRMUserChangePasswordOnFirstAccess(UserManager<ApplicationUser> mUserManager, string userName, string newPassword)
        {
            bool bRet = false;
            try
            {
                if (Program.SystemUsersCreated)
                {
                    //Creazione dell'utente:
                    if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(newPassword))
                    {
                        //Verifico l'esistenza dell'utente
                        Task<ApplicationUser> findTask = Task.Run(() => mUserManager.FindByNameAsync(userName));
                        Task.WaitAll(findTask);
                        if (findTask.Result != null)
                        {

                            ApplicationUser user = findTask.Result;

                            Task<string> resetTokenTask = Task.Run(() => mUserManager.GeneratePasswordResetTokenAsync(user));
                            Task.WaitAll(resetTokenTask);
                            string token = resetTokenTask.Result;

                            Task<IdentityResult> resetPwdTask = Task.Run(() => mUserManager.ResetPasswordAsync(user, token, newPassword));
                            Task.WaitAll(resetPwdTask);

                            if (resetPwdTask.Result.Succeeded)
                            {
                                //E fin qui ho cambiato la password

                                //Ora cambio il bit di email confirmed, che noi usiamo come marker del primo accesso.
                                //Password cambiata -> email confermata -> al prox accesso non mi chiederà più di cambiare pwd
                                if (!user.EmailConfirmed)
                                {

                                    Task<ApplicationUser> findUserTaskAfterReset = Task.Run(() => mUserManager.FindByNameAsync(userName));
                                    Task.WaitAll(findUserTaskAfterReset);
                                    ApplicationUser userAfterReset = findUserTaskAfterReset.Result;

                                    userAfterReset.EmailConfirmed = true;

                                    Task<IdentityResult> updateTask = Task.Run(() => mUserManager.UpdateAsync(userAfterReset));
                                    Task.WaitAll(updateTask);
                                    if (updateTask.Result.Succeeded)
                                    {
                                        bRet = true;
                                    }
                                }
                                else
                                {
                                    bRet = true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                bRet = false;
                Log.Error("SRMUserChangePasswordOnFirstAccess error: " + ex.Message);
            }
            return bRet;
        }

        public long GetIdAuthorFromAuthorName(string name)
        {
            long id = -1;
            try
            {
                DbManager dbManager = new DbManager(Program.CoreIdentity.ConnectionString);
                dbc_BBEIAuthors a = dbManager.GetBBEIAuthorsByName(name);
                if (a != null)
                    id = a.Id;
            }
            catch (Exception ex)
            {
                id = -1;
                Log.Error("GetIdAuthorFromAuthorName error: " + ex.Message);
            }
            return id;
        }

        public string getNameAuthorFromAuthorId(long id)
        {
            string name = "";
            try
            {
                DbManager dbManager = new DbManager(Program.CoreIdentity.ConnectionString);
                dbc_BBEIAuthors a = dbManager.GetBBEIAuthorsById(id);
                if (a != null)
                    name = a.Name;
            }
            catch (Exception ex)
            {
                name = "";
                Log.Error("getNameAuthorFromAuthorId error: " + ex.Message);
            }
            return name;
        }

        public bool AddNewAuthor(string name)
        {
            bool res = false;
            try
            {
                DbManager dbManager = new DbManager(Program.CoreIdentity.ConnectionString);
                Dictionary<string, string> d = new Dictionary<string, string>();
                d.Add("Name", name);
                res = dbManager.InsertRow("dbc_BBEIAuthors", d);
            }
            catch (Exception ex)
            {
                res = false;
                Log.Error("AddNewAuthor error: " + ex.Message);
            }
            return res;
        }

        internal dbc_BBEIFacts getRandomFact()
        {
            dbc_BBEIFacts f = null;
            try
            {
                DbManager dbManager = new DbManager(Program.CoreIdentity.ConnectionString);
                f = dbManager.GetBBEIFactsRandom();
            }
            catch (Exception ex)
            {
                Log.Error("getRandomFact error: " + ex.Message);
            }

            return f;
        }

        internal List<string> getTableList()
        {
            List<string> tables = new List<string>();
            try
            {
                DbManager dbManager = new DbManager(Program.CoreIdentity.ConnectionString);
                tables = dbManager.GetTableNames();
            }
            catch (Exception ex)
            {
                Log.Error("getTableList error: " + ex.Message);
            }

            return tables;
        }


        internal List<Dictionary<string, string>> loadTableData(string tableName, out List<string> tableColumns)
        {
            List<Dictionary<string, string>> tabledata = new List<Dictionary<string, string>>();
            tableColumns = new();
            try
            {
                DbManager dbManager = new DbManager(Program.CoreIdentity.ConnectionString);
                tabledata = dbManager.LoadTableData(tableName, out tableColumns);
            }
            catch (Exception ex)
            {
                Log.Error("loadTableData error: " + ex.Message);
            }
            return tabledata;
        }

        internal bool execQuery(string tableName, string query)
        {
            bool ret = false;
            try
            {
                DbManager dbManager = new DbManager(Program.CoreIdentity.ConnectionString);
                if (query.ToLower().Contains(tableName.ToLower()))
                    ret = dbManager.ExecQuery(query);
            }
            catch (Exception ex)
            {
                ret = false;
                Log.Error("execQuery error: " + ex.Message);
            }
            return ret;
        }

        internal bool updateRow(string tableName, Dictionary<string, string> row)
        {
            bool ret = false;
            try
            {
                DbManager dbManager = new DbManager(Program.CoreIdentity.ConnectionString);
                ret = dbManager.UpdateRow(tableName, row);
            }
            catch (Exception ex)
            {
                ret = false;
                Log.Error("updateRow error: " + ex.Message);
            }
            return ret;
        }

        public bool insertRow(string tableName, Dictionary<string, string> row)
        {
            bool ret = false;
            try
            {
                DbManager dbManager = new DbManager(Program.CoreIdentity.ConnectionString);
                ret = dbManager.InsertRow(tableName, row);
            }
            catch (Exception ex)
            {
                ret = false;
                Log.Error("insertRow error: " + ex.Message);
            }
            return ret;
        }

        public bool deleteRow(string tableName, int id)
        {
            bool ret = false;
            try
            {
                DbManager dbManager = new DbManager(Program.CoreIdentity.ConnectionString);
                ret = dbManager.DeleteRow(tableName, id);
            }
            catch (Exception ex)
            {
                ret = false;
                Log.Error("deleteRow error: " + ex.Message);
            }
            return ret;
        }

    }
}


