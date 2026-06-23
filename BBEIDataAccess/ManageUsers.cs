using Microsoft.AspNetCore.Identity;

namespace BBEIDataAccess
{
    public class ManageUsers
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;


        public ManageUsers(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public bool CreateRole(string rolename, out string message)
        {
            message = "";
            var role = new IdentityRole { Name = rolename };

            Task<IdentityResult> createRoleTask = Task.Run(() => _roleManager.CreateAsync(role));
            Task.WaitAll(createRoleTask);

            if (createRoleTask.Result.Succeeded)
            {
                return true;
            }
            else
            {
                message = string.Join(", ", createRoleTask.Result.Errors.Select(x => x.Description).ToList());
                return false;
            }
        }

        public bool CreateUser(string username, string email, string password, string secEmail, string phoneNumber, string fullName, bool requestPwdChangeOnFirstLogin, out string message)
        {
            message = "";
            var user = new ApplicationUser { UserName = username, Email = email, LockoutEnabled = false, EmailConfirmed = !requestPwdChangeOnFirstLogin, SecondaryEmailAddress = secEmail, PhoneNumber = phoneNumber, Name = fullName };

            Task<IdentityResult> CreateUserTask = Task.Run(() => _userManager.CreateAsync(user, password));
            Task.WaitAll(CreateUserTask);

            var passwordHasher = new PasswordHasher<ApplicationUser>();
            var presult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

            if (CreateUserTask.Result.Succeeded && presult == PasswordVerificationResult.Success)
            {
                return true;
            }
            else
            {
                message = string.Join(", ", CreateUserTask.Result.Errors.Select(x => x.Description).ToList());
                return false;
            }
        }

        public bool CreateUserRole(string username, string rolename, out string message)
        {
            message = "";

            Task<ApplicationUser> findUserTask = Task.Run(() => _userManager.FindByNameAsync(username));
            Task.WaitAll(findUserTask);
            var user = findUserTask.Result;

            Task<IdentityResult> addRoleTask = Task.Run(() => _userManager.AddToRoleAsync(user, rolename));
            Task.WaitAll(addRoleTask);
            var result = addRoleTask.Result;

            if (result.Succeeded)
            {
                return true;
            }
            else
            {
                message = string.Join(", ", result.Errors.Select(x => x.Description).ToList());
                return false;
            }
        }

        public bool ExistsRole(string rolename)
        {
            bool bRet = false;
            try
            {
                Task<IdentityRole> findRole = Task.Run(() => _roleManager.FindByNameAsync(rolename));
                Task.WaitAll(findRole);
                if (findRole.Result != null)
                {
                    bRet = true;
                }
                else
                {
                    bRet = false;
                }
            }
            catch (Exception ex)
            {
                bRet = false;
            }

            return bRet;

        }


        public bool ExistsUser(string username)
        {
            bool bRet = false;
            try
            {
                Task<ApplicationUser> findTask = Task.Run(() => _userManager.FindByNameAsync(username));
                Task.WaitAll(findTask);
                if (findTask.Result != null)
                {
                    bRet = true;
                }
                else
                {
                    bRet = false;
                }

            }
            catch (Exception ex)
            {
                bRet = false;
            }

            return bRet;

        }

        public bool ExistsUserRole(string username, string rolename)
        {
            Task<ApplicationUser> findUserTask = Task.Run(() => _userManager.FindByNameAsync(username));
            Task.WaitAll(findUserTask);
            var user = findUserTask.Result;

            Task<IList<string>> getRolesTask = Task.Run(() => _userManager.GetRolesAsync(user));
            Task.WaitAll(getRolesTask);
            var roles = getRolesTask.Result;

            foreach (var r in roles)
            {
                if (r != null && r.Equals(rolename))
                {
                    return true;
                }
            }
            return false;


        }
    }
}
