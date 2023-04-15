using System.Xml.Linq;

namespace Family.Authentication
{
    public class UserAccountService
    {
        private List<UserAccount> _users;
        public UserAccountService()
        {
            var xdb = XElement.Load("wwwroot/users.xml");
            _users = new List<UserAccount>()
            {
                new UserAccount() { UserName = "admin", Password = "admin", Role = "Administrator" },
                new UserAccount() { UserName = "user", Password = "user", Role = "User" }
            };
            if (xdb != null )
            {
                _users = xdb.Elements("user")
                    .Select(x => new UserAccount()
                    {
                        UserName = x.Element("name")?.Value,
                        Password = x.Element("password")?.Value,
                        Role = x.Element("role")?.Value
                    }).ToList();
            }
        }
        public UserAccount? GetByUserName(string userName)
        {
            return _users.FirstOrDefault(x => x.UserName == userName);
        }
    }
}
