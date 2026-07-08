using Dapper;
using System.Data;
using System.Data.SQLite;
using BBEIDataAccess;
using BBEIDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Reflection;

namespace BBEILib
{
    public class DbManager
    {
        private BBEIDataAccess.IDataBase db;
        private BBEIContext ef_db;

        public DbManager(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BBEIContext>();
            optionsBuilder.UseSqlite(connectionString);
            db = new DataBaseFactory().CreateDatabase("BBEIDataAccess.SqliteDataBase", connectionString);
            ef_db = new BBEIContext(optionsBuilder.Options);
        }

        private IDbConnection? CreateOpenConnectionResiliently()
        {
            IDbConnection? connection = null;
            bool succeeded = false;
            const int totalNumberOfTimesToTry = 10;
            int retryIntervalSeconds = 60;

            for (int tries = 1; tries <= totalNumberOfTimesToTry; tries++)
            {
                try
                {
                    if (tries > 1)
                    {
                        Log.Information("CreateOpenConnectionResiliently: {0} seconds wait before next attempt...", retryIntervalSeconds);
                        Thread.Sleep(1000 * retryIntervalSeconds);
                        Log.Information("CreateOpenConnectionResiliently: attempt number {0} of {1} max starting...", tries, totalNumberOfTimesToTry);
                    }
                    connection = db.CreateOpenConnection();
                    if (connection != null)
                    {
                        succeeded = true;
                        if (tries > 1)
                        {
                            Log.Information("CreateOpenConnectionResiliently: attempt number {0} of {1} max succeeded!", tries, totalNumberOfTimesToTry);
                        }
                        break;
                    }
                }
                catch (Exception e)
                {
                    Log.Information("OpenConnectionResiliently error: on attempt " + tries.ToString() + " of " + totalNumberOfTimesToTry.ToString() + " message: " + e.Message);
                    succeeded = false;
                }
            }

            if (!succeeded)
            {
                Log.Error("CreateOpenConnectionResiliently error: Unable to access the database!");
                connection = null;
            }

            return connection;
        }


        public string GetUserId(string userName)
        {
            string oStr = string.Empty;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    var reader = connection.ExecuteReader("SELECT * FROM AspNetUsers WHERE UserName = '" + userName + "'");
                    while (reader.Read())
                    {
                        oStr = reader.GetString(0);
                    }
                    reader.Close();
                    reader.Dispose();
                }
                else
                {
                    Log.Error("SRMUserAspNetGetIdByName error: Unable to access the database!");
                }
            }
            return oStr;
        }

        public string GetRoleId(string roleName)
        {
            string oStr = string.Empty;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    var reader = connection.ExecuteReader("SELECT * FROM AspNetRoles WHERE Name = '" + roleName + "'");
                    while (reader.Read())
                    {
                        oStr = reader.GetString(0);
                    }
                    reader.Close();
                    reader.Dispose();
                }
                else
                {
                    Log.Error("SRMUserAspNetGetRoleIdByName error: Unable to access the database!");
                }
            }

            return oStr;
        }

        public bool UpdateUserRole(string idUser, string idRole)
        {
            bool bRet = false;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    using (var trans = connection.BeginTransaction())
                    {
                        try
                        {
                            DynamicParameters _params = new DynamicParameters();
                            _params.Add("@Id", idUser, dbType: DbType.String, direction: ParameterDirection.Input);
                            _params.Add("@RoleId", idRole, dbType: DbType.String, direction: ParameterDirection.Input);

                            string updateDetail = "UPDATE AspNetUserRoles SET RoleId = @RoleId WHERE UserId = @Id;\r\n";

                            var res = connection.Execute(sql: updateDetail, param: _params, transaction: trans, commandType: CommandType.Text);
                            if (res == 0)
                            {
                                bRet = false;
                                Log.Error("SRMUserAspNetUpdateRole error: Update execution failed");
                            }
                            else
                            {
                                bRet = true;
                            }

                        }
                        catch (Exception ex)
                        {
                            bRet = false;
                            Log.Error("SRMUserAspNetUpdateRole error: " + ex.Message);
                        }

                        if (bRet)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch (Exception ex)
                            {
                                Log.Error("SRMUserAspNetUpdateRole error: " + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("SRMUserAspNetUpdateRole error: Unable to access the database!");
                }
            }

            return bRet;
        }

        public bool DeleteUser(string userId)
        {
            bool bRet = false;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {

                    using (var trans = connection.BeginTransaction())
                    {
                        string query = "DELETE FROM dbc_FASTeams WHERE AspNetUsersId='" + userId + "'";
                        var res = connection.Execute(sql: query, param: null, transaction: trans, commandType: CommandType.Text);
                        if (res == 0)
                        {
                            trans.Rollback();
                            Log.Error("SRMUserDelete: Delete failed. UserId: " + userId);
                        }
                        else
                        {
                            trans.Commit();
                            Log.Information("SRMUserDelete: Delete OK. UserId: " + userId);
                            bRet = true;
                        }
                    }
                }
                else
                {
                    Log.Error("SRMUserDelete error: Unable to access the database!");
                }
            }
            return bRet;
        }

        public UserInfo GetUserInfoById(string userId)
        {
            UserInfo dui = new UserInfo();

            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    var reader = connection.ExecuteReader("SELECT AspNetUsers.Id AS Expr1, AspNetUsers.UserName, AspNetUsers.SecondaryEmailAddress, AspNetUsers.PhoneNumber, AspNetUsers.Name, AspNetRoles.Id, AspNetRoles.Name FROM AspNetRoles INNER JOIN AspNetUserRoles ON AspNetRoles.Id = AspNetUserRoles.RoleId INNER JOIN AspNetUsers ON AspNetUserRoles.UserId = AspNetUsers.Id where AspNetUsers.Id = '" + userId + "'");
                    while (reader.Read())
                    {
                        dui.UserId = reader.GetString(0);
                        dui.UserName = reader.GetString(1);
                        dui.SecondaryEmail = !reader.IsDBNull(2) ? reader.GetString(2) : "";
                        dui.PhoneNumber = !reader.IsDBNull(3) ? reader.GetString(3) : "";
                        dui.Name = !reader.IsDBNull(4) ? reader.GetString(4) : "";
                        dui.RoleId = reader.GetString(5);
                        dui.Role = reader.GetString(6);

                    }
                    reader.Close();
                    reader.Dispose();
                }
                else
                {
                    Log.Error("GetUserInfoById error: Unable to access the database!");
                }
            }

            return dui;
        }

        public List<UserInfo> GetUsers(bool imSystemUser, string username = "")
        {
            List<UserInfo> totList = new List<UserInfo>();

            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    try
                    {
                        string query = "SELECT AspNetUsers.Id AS Expr1, AspNetUsers.UserName, AspNetUsers.SecondaryEmailAddress, AspNetUsers.PhoneNumber, AspNetUsers.Name, AspNetRoles.Id, AspNetRoles.Name FROM AspNetRoles INNER JOIN AspNetUserRoles ON AspNetRoles.Id = AspNetUserRoles.RoleId INNER JOIN AspNetUsers ON AspNetUserRoles.UserId = AspNetUsers.Id";
                        if (!string.IsNullOrEmpty(username))
                            query += " WHERE AspNetUsers.UserName = '" + username + "'";
                        var reader = connection.ExecuteReader(query);
                        while (reader.Read())
                        {
                            UserInfo dui = new();
                            dui.UserId = reader.GetString(0);
                            dui.UserName = reader.GetString(1);
                            dui.SecondaryEmail = !reader.IsDBNull(2) ? reader.GetString(2) : "";
                            dui.PhoneNumber = !reader.IsDBNull(3) ? reader.GetString(3) : "";
                            dui.Name = !reader.IsDBNull(4) ? reader.GetString(4) : "";
                            dui.RoleId = reader.GetString(5);
                            dui.Role = reader.GetString(6);

                            //escludo le identità di sistema
                            //Compilo i customers abilitati su quell'utenza
                            //List<string> custList = SRMGetEnabledCustomersForUserId(dui.UserId);
                            //dui.Customers = custList;
                            //e aggiungo
                            totList.Add(dui);
                        }
                        reader.Close();
                        reader.Dispose();
                    }
                    catch (Exception e)
                    {
                        Log.Information("GetUsers error: message: " + e.Message);
                    }
                }
                else
                {
                    Log.Error("GetUsers error: Unable to access the database!");
                }
            }

            return totList;
        }



        public List<dbc_BBEIAuthors> GetBBEIAuthorsByIds(List<long> list)
        {
            List<dbc_BBEIAuthors> totList = new List<dbc_BBEIAuthors>();
            totList = ef_db.dbc_BBEIAuthors.Where(x => list.Contains(x.Id)).ToList();
            return totList;
        }

        public dbc_BBEIAuthors GetBBEIAuthorsById(long id)
        {
            dbc_BBEIAuthors l = new dbc_BBEIAuthors();
            l = ef_db.dbc_BBEIAuthors.First(x => x.Id == id);
            return l;
        }

        public dbc_BBEIAuthors GetBBEIAuthorsByName(string name)
        {
            dbc_BBEIAuthors l = new dbc_BBEIAuthors();
            l = ef_db.dbc_BBEIAuthors.FirstOrDefault(x => x.Name.ToLower().Trim() == name.ToLower().Trim());
            return l;
        }

        public bool SetFASTeamNew(string userId, string teamName, string teamShortName, bool active, out long teamId)
        {
            bool bRet = false;
            bool error = false;
            teamId = -1;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    using (var trans = connection.BeginTransaction())
                    {
                        try
                        {
                            DynamicParameters _params = new DynamicParameters();
                            _params.Add("@AspNetUsersId", userId, dbType: DbType.String, direction: ParameterDirection.Input);
                            _params.Add("@Name", teamName, dbType: DbType.String, direction: ParameterDirection.Input);
                            _params.Add("@ShortName", teamShortName, dbType: DbType.String, direction: ParameterDirection.Input);
                            _params.Add("@Active", active, dbType: DbType.Boolean, direction: ParameterDirection.Input);
                            _params.Add("@DtCreation", DateTime.Now, dbType: DbType.DateTime, direction: ParameterDirection.Input);

                            string insertSql = "INSERT INTO dbc_BBEITeams (AspNetUsersId, Name, ShortName, Active, DtCreation) VALUES (@AspNetUsersId, @Name, @ShortName, @Active, @DtCreation)";
                            string selectSql = "SELECT last_insert_rowid();";

                            var res = connection.Execute(sql: insertSql, param: _params, transaction: trans, commandType: CommandType.Text);
                            teamId = connection.ExecuteScalar<long>(sql: selectSql, transaction: trans, commandType: CommandType.Text);
                            if (res == 0)
                            {
                                error = true;
                                Log.Error(MethodBase.GetCurrentMethod().Name, "DB Error write dbc_BBEITeams", null);
                            }
                            else
                            {
                                bRet = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            bRet = false;
                            Log.Error("SRMUserAspNetUpdateRole error: " + ex.Message);
                        }

                        if (bRet)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch (Exception ex)
                            {
                                Log.Error("SRMUserAspNetUpdateRole error: " + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("SRMUserAspNetUpdateRole error: Unable to access the database!");
                }
            }

            return bRet;
        }

        public bool SetFASTeamActivation(long team2activate, bool v)
        {
            bool bRet = false;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    using (var trans = connection.BeginTransaction())
                    {
                        try
                        {
                            DynamicParameters _params = new DynamicParameters();
                            _params.Add("@Active", v, dbType: DbType.Boolean, direction: ParameterDirection.Input);
                            _params.Add("@Id", team2activate, dbType: DbType.String, direction: ParameterDirection.Input);

                            string updateDetail = "UPDATE dbc_BBEITeams SET Active = @Active WHERE Id = @Id";

                            var res = connection.Execute(sql: updateDetail, param: _params, transaction: trans, commandType: CommandType.Text);
                            if (res == 0)
                            {
                                bRet = false;
                                Log.Error("SetFASTeamActivation error: Update execution failed");
                            }
                            else
                            {
                                bRet = true;
                            }

                        }
                        catch (Exception ex)
                        {
                            bRet = false;
                            Log.Error("SetFASTeamActivation error: " + ex.Message);
                        }

                        if (bRet)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch (Exception ex)
                            {
                                Log.Error("SetFASTeamActivation error: " + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("SetFASTeamActivation error: Unable to access the database!");
                }
            }

            return bRet;
        }

        public bool DeleteFASLeagueById(long id)
        {
            bool bRet = false;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {

                    using (var trans = connection.BeginTransaction())
                    {
                        string query = "DELETE FROM dbc_BBEILeagues WHERE Id=" + id + " ";
                        var res = connection.Execute(sql: query, param: null, transaction: trans, commandType: CommandType.Text);
                        if (res == 0)
                        {
                            trans.Rollback();
                            Log.Error("dbc_BBEILeagues: Delete failed. leagueId: " + id);
                        }
                        else
                        {
                            trans.Commit();
                            Log.Information("dbc_BBEILeagues: Delete OK. leagueId: " + id);
                            bRet = true;
                        }
                    }
                }
                else
                {
                    Log.Error("dbc_BBEILeagues error: Unable to access the database!");
                }
            }
            return bRet;
        }
       
        public bool UpdateFASLeagueById(long id, string name, string description, int credits, int NPor, int NDif, int NCen, int NAtt)
        {
            bool bRet = false;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    using (var trans = connection.BeginTransaction())
                    {
                        try
                        {
                            DynamicParameters _params = new DynamicParameters();
                            _params.Add("@Name", name, dbType: DbType.String, direction: ParameterDirection.Input);
                            _params.Add("@Description", description, dbType: DbType.String, direction: ParameterDirection.Input);
                            _params.Add("@Credits", credits, dbType: DbType.Int32, direction: ParameterDirection.Input);
                            _params.Add("@NPor", NPor, dbType: DbType.Int32, direction: ParameterDirection.Input);
                            _params.Add("@NDif", NDif, dbType: DbType.Int32, direction: ParameterDirection.Input);
                            _params.Add("@NCen", NCen, dbType: DbType.Int32, direction: ParameterDirection.Input);
                            _params.Add("@NAtt", NAtt, dbType: DbType.Int32, direction: ParameterDirection.Input);
                            _params.Add("@Id", id, dbType: DbType.Int64, direction: ParameterDirection.Input);

                            string updateDetail = "UPDATE dbc_BBEILeagues SET Name = @Name, Description = @Description, Credits = @Credits, NPor=@NPor, NDif=@NDif, NCen=@NCen, NAtt=@NAtt WHERE Id = @Id;";

                            var res = connection.Execute(sql: updateDetail, param: _params, transaction: trans, commandType: CommandType.Text);

                            if (res == 0)
                            {
                                bRet = false;
                                Log.Error("UpdateFASLeagueById error: Update execution failed");
                            }
                            else
                            {
                                bRet = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            bRet = false;
                            Log.Error("UpdateFASLeagueById error: " + ex.Message);
                        }

                        if (bRet)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch (Exception ex)
                            {
                                Log.Error("UpdateFASLeagueById error: " + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("UpdateFASLeagueById error: Unable to access the database!");
                }
            }

            return bRet;
        }

        
        public bool InsertNewStanding(long idCompetition, Dictionary<long, int> teams)
        {
            bool bRet = false;
            bool error = false;

            long ret = -1;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    using (var trans = connection.BeginTransaction())
                    {
                        try
                        {
                            var valuesList = new List<string>();
                            var parameters = new DynamicParameters();

                            string insertSql = "INSERT INTO dbc_BBEIStandings (IdFASCompetition, IdFASTeam, CompetitionGroup, Points, Played, Won, Draw, Lost, ScoredGoal, ConcededGoal, EndOfRegularSeason, Active, Retro) VALUES ";
                            string selectSql = "SELECT last_insert_rowid();";

                            int index = 0;
                            foreach (var team in teams)
                            {
                                valuesList.Add($"(@IdFASCompetition{index}, @IdFASTeam{index}, @CompetitionGroup{index}, @Points{index}, @Played{index}, @Won{index}, @Draw{index}, @Lost{index}, @ScoredGoal{index}, @ConcededGoal{index}, @EndOfRegularSeason{index}, @Active{index}, @Retro{index})");
                                parameters.Add($"@IdFASCompetition{index}", idCompetition, DbType.Int64);
                                parameters.Add($"@IdFASTeam{index}", team.Key, DbType.Int64);
                                parameters.Add($"@CompetitionGroup{index}", team.Value, DbType.Int32);
                                parameters.Add($"@Points{index}", 0, DbType.Int32);
                                parameters.Add($"@Played{index}", 0, DbType.Int32);
                                parameters.Add($"@Won{index}", 0, DbType.Int32);
                                parameters.Add($"@Draw{index}", 0, DbType.Int32);
                                parameters.Add($"@Lost{index}", 0, DbType.Int32);
                                parameters.Add($"@ScoredGoal{index}", 0, DbType.Int32);
                                parameters.Add($"@ConcededGoal{index}", 0, DbType.Int32);
                                parameters.Add($"@EndOfRegularSeason{index}", false, DbType.Boolean);
                                parameters.Add($"@Active{index}", true, DbType.Boolean);
                                parameters.Add($"@Retro{index}", false, DbType.Boolean);
                                index++;
                            }

                            insertSql += string.Join(", ", valuesList) + ";";

                            connection.Execute(insertSql, parameters, transaction: trans);
                            ret = connection.ExecuteScalar<long>(sql: selectSql, transaction: trans, commandType: CommandType.Text);

                            if (ret == 0)
                            {
                                error = true;
                                Log.Error(MethodBase.GetCurrentMethod().Name, "DB Error write dbc_SRATeams", null);
                            }
                            else
                            {
                                bRet = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            bRet = false;
                            Log.Error("dbc_SRATeams error: " + ex.Message);
                        }

                        if (bRet)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch (Exception ex)
                            {
                                Log.Error("dbc_SRATeams error: " + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("dbc_SRATeams error: Unable to access the database!");
                }
            }

            return bRet;
        }

        public bool UpdateFASStandingById(long id, int totPoints, int totPlayed, int totWon, int totDraw, int totLost, int totScoredGoals, int totConcededGoals)
        {
            bool bRet = false;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    using (var trans = connection.BeginTransaction())
                    {
                        try
                        {
                            DynamicParameters _params = new DynamicParameters();
                            _params.Add("@Points", totPoints, dbType: DbType.Int32, direction: ParameterDirection.Input);
                            _params.Add("@Played", totPlayed, dbType: DbType.Int32, direction: ParameterDirection.Input);
                            _params.Add("@Won", totWon, dbType: DbType.Int32, direction: ParameterDirection.Input);
                            _params.Add("@Draw", totDraw, dbType: DbType.Int32, direction: ParameterDirection.Input);
                            _params.Add("@Lost", totLost, dbType: DbType.Int32, direction: ParameterDirection.Input);
                            _params.Add("@ScoredGoal", totScoredGoals, dbType: DbType.Int32, direction: ParameterDirection.Input);
                            _params.Add("@ConcededGoal", totConcededGoals, dbType: DbType.Int32, direction: ParameterDirection.Input);
                            _params.Add("@Id", id, dbType: DbType.Int64, direction: ParameterDirection.Input);

                            string updateDetail = "UPDATE dbc_BBEIStandings SET Points = @Points, Played = @Played, Won = @Won, Draw=@Draw, Lost=@Lost, ScoredGoal=@ScoredGoal, ConcededGoal=@ConcededGoal WHERE Id = @Id;";

                            var res = connection.Execute(sql: updateDetail, param: _params, transaction: trans, commandType: CommandType.Text);

                            if (res == 0)
                            {
                                bRet = false;
                                Log.Error("UpdateFASStandingById error: Update execution failed");
                            }
                            else
                            {
                                bRet = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            bRet = false;
                            Log.Error("UpdateFASStandingById error: " + ex.Message);
                        }

                        if (bRet)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch (Exception ex)
                            {
                                Log.Error("UpdateFASStandingById error: " + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("UpdateFASStandingById error: Unable to access the database!");
                }
            }

            return bRet;
        }

        public bool UpdateFASRoundCalculatedById(long idFASRound, bool calculated, double HomePoints, double AwayPoints, int HomeGoal, int AwayGoal, int HomePenality, int AwayPenality, int HomeExtra, int AwayExtra)
        {
            bool bRet = false;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    using (var trans = connection.BeginTransaction())
                    {
                        try
                        {
                            DynamicParameters _params = new DynamicParameters();
                            _params.Add("@Calculated", calculated, dbType: DbType.Boolean, direction: ParameterDirection.Input);

                            _params.Add("@HomePoints", HomePoints, dbType: DbType.Double, direction: ParameterDirection.Input);
                            _params.Add("@AwayPoints", AwayPoints, dbType: DbType.Double, direction: ParameterDirection.Input);
                            _params.Add("@HomeGoal", HomeGoal, dbType: DbType.Int32, direction: ParameterDirection.Input);
                            _params.Add("@AwayGoal", AwayGoal, dbType: DbType.Int32, direction: ParameterDirection.Input);
                            _params.Add("@HomePenalty", HomePenality, dbType: DbType.Int32, direction: ParameterDirection.Input);
                            _params.Add("@AwayPenalty", AwayPenality, dbType: DbType.Int32, direction: ParameterDirection.Input);
                            _params.Add("@HomeExtra", HomeExtra, dbType: DbType.Int32, direction: ParameterDirection.Input);
                            _params.Add("@AwayExtra", AwayExtra, dbType: DbType.Int32, direction: ParameterDirection.Input);

                            _params.Add("@Id", idFASRound, dbType: DbType.Int64, direction: ParameterDirection.Input);

                            string updateDetail = "UPDATE dbc_BBEIRounds SET Calculated=@Calculated, HomePoints=@HomePoints, AwayPoints=@AwayPoints, HomeGoal=@HomeGoal, AwayGoal=@AwayGoal, HomePenalty=@HomePenalty, AwayPenalty=@AwayPenalty, HomeExtra=@HomeExtra, AwayExtra=@AwayExtra WHERE Id = @Id;";

                            var res = connection.Execute(sql: updateDetail, param: _params, transaction: trans, commandType: CommandType.Text);

                            if (res == 0)
                            {
                                bRet = false;
                                Log.Error("UpdateFASRoundCalculatedById error: Update execution failed");
                            }
                            else
                            {
                                bRet = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            bRet = false;
                            Log.Error("UpdateFASRoundCalculatedById error: " + ex.Message);
                        }

                        if (bRet)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch (Exception ex)
                            {
                                Log.Error("UpdateFASRoundCalculatedById error: " + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("UpdateFASRoundCalculatedById error: Unable to access the database!");
                }
            }

            return bRet;
        }

        public bool UpdateFASRoundStandingMovedById(long idFASRound, bool standingMoved)
        {
            bool bRet = false;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    using (var trans = connection.BeginTransaction())
                    {
                        try
                        {
                            DynamicParameters _params = new DynamicParameters();
                            _params.Add("@StandingMoved", standingMoved, dbType: DbType.Boolean, direction: ParameterDirection.Input);
                            _params.Add("@Id", idFASRound, dbType: DbType.Int64, direction: ParameterDirection.Input);

                            string updateDetail = "UPDATE dbc_BBEIRounds SET StandingMoved = @StandingMoved WHERE Id = @Id;";

                            var res = connection.Execute(sql: updateDetail, param: _params, transaction: trans, commandType: CommandType.Text);

                            if (res == 0)
                            {
                                bRet = false;
                                Log.Error("UpdateFASRoundStandingMovedById error: Update execution failed");
                            }
                            else
                            {
                                bRet = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            bRet = false;
                            Log.Error("UpdateFASRoundStandingMovedById error: " + ex.Message);
                        }

                        if (bRet)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch (Exception ex)
                            {
                                Log.Error("UpdateFASRoundStandingMovedById error: " + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("UpdateFASRoundStandingMovedById error: Unable to access the database!");
                }
            }

            return bRet;
        }

        public bool UpdateFASStandingEndOfSeasonByIdFASCompetition(long idFASCompetition)
        {
            bool bRet = false;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    using (var trans = connection.BeginTransaction())
                    {
                        try
                        {
                            DynamicParameters _params = new DynamicParameters();
                            _params.Add("@EndOfRegularSeason", true, dbType: DbType.Boolean, direction: ParameterDirection.Input);
                            _params.Add("@IdFASCompetition", idFASCompetition, dbType: DbType.Int64, direction: ParameterDirection.Input);

                            string updateDetail = "UPDATE dbc_BBEIStandings SET EndOfRegularSeason = @EndOfRegularSeason WHERE IdFASCompetition = @IdFASCompetition;";

                            var res = connection.Execute(sql: updateDetail, param: _params, transaction: trans, commandType: CommandType.Text);

                            if (res == 0)
                            {
                                bRet = false;
                                Log.Error("UpdateFASStandingEndOfSeasonByIdFASCompetition error: Update execution failed");
                            }
                            else
                            {
                                bRet = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            bRet = false;
                            Log.Error("UpdateFASStandingEndOfSeasonByIdFASCompetition error: " + ex.Message);
                        }

                        if (bRet)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch (Exception ex)
                            {
                                Log.Error("UpdateFASStandingEndOfSeasonByIdFASCompetition error: " + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("UpdateFASStandingEndOfSeasonByIdFASCompetition error: Unable to access the database!");
                }
            }

            return bRet;
        }

        public bool UpdateFASTeamRetroByIdCompetitionIdTeam(long idFASCompetition, long idFASTeam)
        {
            bool bRet = false;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    using (var trans = connection.BeginTransaction())
                    {
                        try
                        {
                            DynamicParameters _params = new DynamicParameters();
                            _params.Add("@Retro", true, dbType: DbType.Boolean, direction: ParameterDirection.Input);
                            _params.Add("@IdFASCompetition", idFASCompetition, dbType: DbType.Int64, direction: ParameterDirection.Input);
                            _params.Add("@idFASTeam", idFASTeam, dbType: DbType.Int64, direction: ParameterDirection.Input);

                            string updateDetail = "UPDATE dbc_BBEIStandings SET Retro = @Retro WHERE IdFASCompetition = @IdFASCompetition and idFASTeam = @idFASTeam;";

                            var res = connection.Execute(sql: updateDetail, param: _params, transaction: trans, commandType: CommandType.Text);

                            if (res == 0)
                            {
                                bRet = false;
                                Log.Error("UpdateFASTeamRetroByIdCompetitionIdTeam error: Update execution failed");
                            }
                            else
                            {
                                bRet = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            bRet = false;
                            Log.Error("UpdateFASTeamRetroByIdCompetitionIdTeam error: " + ex.Message);
                        }

                        if (bRet)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch (Exception ex)
                            {
                                Log.Error("UpdateFASTeamRetroByIdCompetitionIdTeam error: " + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("UpdateFASTeamRetroByIdCompetitionIdTeam error: Unable to access the database!");
                }
            }

            return bRet;
        }

        public bool UpdateWinneingTeamOnFASCompetitionsByIdCompetition(long idFASCompetition, long idFASTeam)
        {
            bool bRet = false;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    using (var trans = connection.BeginTransaction())
                    {
                        try
                        {
                            DynamicParameters _params = new DynamicParameters();
                            _params.Add("@IdFASCompetition", idFASCompetition, dbType: DbType.Int64, direction: ParameterDirection.Input);
                            _params.Add("@IdFASTeamWinner", idFASTeam, dbType: DbType.Int64, direction: ParameterDirection.Input);

                            string updateDetail = "UPDATE dbc_BBEICompetitions SET IdFASTeamWinner = @IdFASTeamWinner WHERE Id = @IdFASCompetition;";

                            var res = connection.Execute(sql: updateDetail, param: _params, transaction: trans, commandType: CommandType.Text);

                            if (res == 0)
                            {
                                bRet = false;
                                Log.Error("UpdateWinneingTeamOnFASCompetitionsByIdCompetition error: Update execution failed");
                            }
                            else
                            {
                                bRet = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            bRet = false;
                            Log.Error("UpdateWinneingTeamOnFASCompetitionsByIdCompetition error: " + ex.Message);
                        }

                        if (bRet)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch (Exception ex)
                            {
                                Log.Error("UpdateWinneingTeamOnFASCompetitionsByIdCompetition error: " + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("UpdateWinneingTeamOnFASCompetitionsByIdCompetition error: Unable to access the database!");
                }
            }

            return bRet;
        }

        public List<string> GetTableNames()
        {
            var tableNames = new List<string>();
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    var reader = connection.ExecuteReader("SELECT name FROM sqlite_master WHERE type='table'");
                    while (reader.Read())
                    {
                        tableNames.Add(reader.GetString(0));
                    }
                    reader.Close();
                    reader.Dispose();
                }
                else
                {
                    Log.Error("GetTableNamesAsync error: Unable to access the database!");
                }
            }

            return tableNames;
        }
        public bool ExecQuery(string query)
        {
            bool bRet = false;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    using (var trans = connection.BeginTransaction())
                    {
                        try
                        {
                            var res = connection.Execute(sql: query, transaction: trans, commandType: CommandType.Text);

                            if (res == 0)
                            {
                                bRet = false;
                                Log.Error("ExecQuery error: Execution failed");
                            }
                            else
                            {
                                bRet = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            bRet = false;
                            Log.Error("ExecQuery error: " + ex.Message);
                        }

                        if (bRet)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch (Exception ex)
                            {
                                Log.Error("ExecQuery error: " + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("ExecQuery error: Unable to access the database!");
                }
            }

            return bRet;
        }

        public bool DeleteFASStandingByIdCompetition(long idCompetition)
        {
            bool bRet = false;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    using (var trans = connection.BeginTransaction())
                    {
                        string query = "DELETE FROM dbc_BBEIStandings WHERE IdFASCompetition=" + idCompetition + " ";
                        var res = connection.Execute(sql: query, param: null, transaction: trans, commandType: CommandType.Text);
                        if (res == 0)
                        {
                            trans.Rollback();
                            Log.Error("dbc_BBEIStandings: Delete failed. idCompetition: " + idCompetition);
                        }
                        else
                        {
                            trans.Commit();
                            Log.Information("dbc_BBEIStandings: Delete OK. idCompetition: " + idCompetition);
                            bRet = true;
                        }
                    }
                }
                else
                {
                    Log.Error("dbc_BBEIStandings error: Unable to access the database!");
                }
            }
            return bRet;
        }

        public bool UpdateFASRoundStandingMovedByIdCompetition(long idCompetition, bool standingMoved)
        {
            bool bRet = false;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    using (var trans = connection.BeginTransaction())
                    {
                        try
                        {
                            DynamicParameters _params = new DynamicParameters();
                            _params.Add("@StandingMoved", standingMoved, dbType: DbType.Boolean, direction: ParameterDirection.Input);
                            _params.Add("@IdFASCompetition", idCompetition, dbType: DbType.Int64, direction: ParameterDirection.Input);

                            string updateDetail = "UPDATE dbc_BBEIRounds SET StandingMoved = @StandingMoved WHERE IdFASCompetition = @IdFASCompetition;";

                            var res = connection.Execute(sql: updateDetail, param: _params, transaction: trans, commandType: CommandType.Text);

                            if (res == 0)
                            {
                                bRet = false;
                                Log.Error("UpdateFASRoundStandingMovedByIdCompetition error: Update execution failed");
                            }
                            else
                            {
                                bRet = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            bRet = false;
                            Log.Error("UpdateFASRoundStandingMovedByIdCompetition error: " + ex.Message);
                        }

                        if (bRet)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch (Exception ex)
                            {
                                Log.Error("UpdateFASRoundStandingMovedByIdCompetition error: " + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("UpdateFASRoundStandingMovedByIdCompetition error: Unable to access the database!");
                }
            }

            return bRet;
        }

        public dbc_BBEIFacts GetBBEIFactsRandom()
        {
            dbc_BBEIFacts l = new dbc_BBEIFacts();
            l = ef_db.dbc_BBEIFacts.OrderBy(x => x).First();
            return l;
        }

        public List<Dictionary<string, string>> LoadTableData(string tableName, out List<string> tableColumns)
        {
            var tableData = new List<Dictionary<string, string>>();
            tableColumns = new();
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    var reader = connection.ExecuteReader($"PRAGMA table_info({tableName})");
                    while (reader.Read())
                    {
                        tableColumns.Add(reader.GetString(1));
                    }

                    reader = connection.ExecuteReader($"SELECT * FROM {tableName}");
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, string>();
                        foreach (var column in tableColumns)
                        {
                            row[column] = reader[column].ToString();
                        }
                        tableData.Add(row);
                    }

                    reader.Close();
                    reader.Dispose();
                }
                else
                {
                    Log.Error("GetTableNamesAsync error: Unable to access the database!");
                }
            }

            return tableData;
        }

        public bool UpdateRow(string tableName, Dictionary<string, string> row)
        {
            bool bRet = false;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    using (var trans = connection.BeginTransaction())
                    {
                        try
                        {
                            var setClauses = new List<string>();
                            DynamicParameters _params = new DynamicParameters();

                            foreach (var column in row.Keys)
                            {
                                if (column != "Id") // Assuming "Id" is the primary key
                                    setClauses.Add($"{column} = @{column}");

                                _params.Add($"@{column}", row[column], direction: ParameterDirection.Input);
                            }

                            string updateDetail = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE Id = @Id";

                            var res = connection.Execute(sql: updateDetail, param: _params, transaction: trans, commandType: CommandType.Text);
                            if (res == 0)
                            {
                                bRet = false;
                                Log.Error("UpdateRow error: Update execution failed");
                            }
                            else
                            {
                                bRet = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            bRet = false;
                            Log.Error("UpdateRow error: " + ex.Message);
                        }

                        if (bRet)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch (Exception ex)
                            {
                                Log.Error("UpdateRow error: " + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("UpdateRow error: Unable to access the database!");
                }
            }

            return bRet;
        }

        public bool InsertRow(string tableName, Dictionary<string, string> row)
        {
            bool bRet = false;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    using (var trans = connection.BeginTransaction())
                    {
                        try
                        {
                            DynamicParameters _params = new DynamicParameters();
                            var columns = string.Join(", ", row.Keys.Where(x => x != "Id").ToList());
                            var values = string.Join(", ", row.Keys.Where(x => x != "Id").ToList().Select(k => $"@{k}"));
                            string insertDetail = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";

                            foreach (var kvp in row)
                            {
                                _params.Add($"@{kvp.Key}", kvp.Value, direction: ParameterDirection.Input);
                            }

                            var res = connection.Execute(sql: insertDetail, param: _params, transaction: trans, commandType: CommandType.Text);
                            if (res == 0)
                            {
                                bRet = false;
                                Log.Error("InsertRow error: Insert execution failed");
                            }
                            else
                            {
                                bRet = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            bRet = false;
                            Log.Error("InsertRow error: " + ex.Message);
                        }

                        if (bRet)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch (Exception ex)
                            {
                                Log.Error("InsertRow error: " + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("InsertRow error: Unable to access the database!");
                }
            }

            return bRet;
        }

        public bool DeleteRow(string tableName, int id)
        {
            bool bRet = false;
            using (IDbConnection? connection = CreateOpenConnectionResiliently())
            {
                if (connection != null)
                {
                    using (var trans = connection.BeginTransaction())
                    {
                        try
                        {
                            DynamicParameters _params = new DynamicParameters();
                            _params.Add("@Id", id, dbType: DbType.Int64, direction: ParameterDirection.Input);

                            string deleteDetail = $"DELETE FROM {tableName} WHERE Id = @Id";

                            var res = connection.Execute(sql: deleteDetail, param: _params, transaction: trans, commandType: CommandType.Text);
                            if (res == 0)
                            {
                                bRet = false;
                                Log.Error("DeleteRow error: Insert execution failed");
                            }
                            else
                            {
                                bRet = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            bRet = false;
                            Log.Error("DeleteRow error: " + ex.Message);
                        }

                        if (bRet)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch (Exception ex)
                            {
                                Log.Error("DeleteRow error: " + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("DeleteRow error: Unable to access the database!");
                }
            }

            return bRet;
        }
    }
}
