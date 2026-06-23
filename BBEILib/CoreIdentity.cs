namespace BBEILib
{
    public class CoreIdentity
    {
        public string BBEIAdminUser { get; set; }
        public string BBEIAdminPassword { get; set; }
        public string ConnectionString { get; set; }
        public CoreIdentity()
        {
            ConnectionString = string.Empty;
            BBEIAdminUser = string.Empty;
            BBEIAdminPassword = string.Empty;
        }
    }
}
