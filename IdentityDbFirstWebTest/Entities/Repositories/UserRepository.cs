using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections;
using IdentityDbFirstWebTest.Entities;

namespace IdentityDbFirstWebTest.Entities.Repositories
{
    public class UserRepository<TUser> :
        IUserStore<TUser, int>, 
        IUserLoginStore<TUser, int>, 
        IUserClaimStore<TUser, int>, 
        IUserRoleStore<TUser, int>, 
        IUserPasswordStore<TUser, int>, 
        IUserSecurityStampStore<TUser, int>,
        IUserLockoutStore<TUser, int>, 
        IUserTwoFactorStore<TUser, int>,
        IUserPhoneNumberStore<TUser, int>
        where TUser : User
    {
        private IdentityTestEntities context; 

        public UserRepository(IdentityTestEntities context)
        {
            this.context = context; 
        }

        public async System.Threading.Tasks.Task CreateAsync(TUser user)
        {
            if (user.Id == 0)
            {
                this.context.Users.Add(user);
                await this.context.SaveChangesAsync();

            }
        }

        public async Task DeleteAsync(TUser user)
        {
            this.context.Users.Remove(user);
            await this.context.SaveChangesAsync();
        }


        public async System.Threading.Tasks.Task<TUser> FindByIdAsync(int userId)
        {
            User user = context.Users.FirstOrDefault(u => u.Id == userId);
            TUser result = user as TUser;


            if (result != null)
            {
                return await Task.FromResult<TUser>(result);
            }

            return await Task.FromResult<TUser>(null);

        }

        public async Task<TUser> FindByNameAsync(string email)
        {

            User user = context.Users.FirstOrDefault(u => u.Email == email);
            TUser result = user as TUser;
           

            if (result != null)
            {
                return await Task.FromResult<TUser>(result);
            }

            return await Task.FromResult<TUser>(null);
        }

        public async Task UpdateAsync(TUser user)
        {
            User _dbUser = this.context.Users.SingleOrDefault<User>(u => u.Id == user.Id);
     
            if (_dbUser != null)
            {
              
                _dbUser.Email = user.Email;
                _dbUser.LockoutEnabled = user.LockoutEnabled;

                await this.context.SaveChangesAsync();
            } 
        }


        public Task AddLoginAsync(TUser user, UserLoginInfo loginInfo)
        {
            if (user == null || loginInfo == null)
            {
                throw new ArgumentNullException("User of LoginInfo does not exists");
            }

            UserLogin login = new UserLogin();
            login.LoginProvider = Int32.Parse(loginInfo.LoginProvider);
            login.ProviderKey = loginInfo.ProviderKey; 
            login.User = user;
            login.UserId = user.Id;

            this.context.UserLogins.Add(login);
            this.context.SaveChangesAsync(); 

            return Task.FromResult<object>(null);
        }

        public async System.Threading.Tasks.Task<TUser> FindAsync(UserLoginInfo login)
        {

            var query = from u in this.context.Users
                        join l in this.context.UserLogins
                        on u.Id equals l.UserId
                        where l.LoginProvider == Int32.Parse(login.LoginProvider) && l.ProviderKey == login.ProviderKey
                        select u; 

            User user = query.AsEnumerable<User>().FirstOrDefault<User>();

            if (user == null)
            {
                return await Task.FromResult<TUser>(null);
            }
            else
            {
                TUser result = user as TUser;

                if (result != null)
                {
                    return await Task.FromResult<TUser>(result);
                }

                return await Task.FromResult<TUser>(null);
            }

        }

        public async System.Threading.Tasks.Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user)
        {
            if(user == null)
            {
                throw new ArgumentException("User"); 
            }

            IEnumerable userLogins = this.context.UserLogins.Where<UserLogin>(ul => ul.UserId == user.Id);

            List<UserLoginInfo> loginInfoList = new List<UserLoginInfo>(); 

            foreach(UserLogin userLogin in userLogins)
            {
                loginInfoList.Add(new UserLoginInfo(userLogin.LoginProvider.ToString(), userLogin.ProviderKey)); 
            }

            return await Task.FromResult<IList<UserLoginInfo>>(loginInfoList);

        }

        public System.Threading.Tasks.Task RemoveLoginAsync(TUser user, UserLoginInfo loginInfo)
        {
            UserLogin userLogin = this.context.UserLogins.
                Where<UserLogin>(ul => ul.LoginProvider == Int32.Parse(loginInfo.LoginProvider) && ul.ProviderKey == loginInfo.ProviderKey && ul.UserId == user.Id).FirstOrDefault<UserLogin>();

            if(userLogin != null)
            {
                this.context.UserLogins.Remove(userLogin);
                this.context.SaveChangesAsync();
            }

            return Task.FromResult<TUser>(null);

        }

        public System.Threading.Tasks.Task AddClaimAsync(TUser user, System.Security.Claims.Claim claim)
        {
            UserClaim userClaim = new UserClaim()
            {
                UserId = user.Id,
                ClaimType = claim.Type,
                ClaimValue = claim.ValueType
            };

            this.context.UserClaims.Add(userClaim);
            this.context.SaveChangesAsync();

            return Task.FromResult<TUser>(null);

        }

        public async Task<IList<Claim>> GetClaimsAsync(TUser user)
        {
            if (user == null)
            {
                throw new ArgumentException("User");
            }

            List<UserClaim> userClaimsList = (from c in this.context.UserClaims
                                           where c.UserId == user.Id
                                           select c).ToList<UserClaim>();

            IList<Claim> claims = new List<Claim>();

            userClaimsList.ForEach(userClaim => claims.Add(new Claim(userClaim.ClaimType, userClaim.ClaimValue))); 

            return await Task.FromResult<IList<Claim>>(claims);
        }

        public async System.Threading.Tasks.Task RemoveClaimAsync(TUser user, System.Security.Claims.Claim claim)
        {
            UserClaim userClaim = (from c in this.context.UserClaims
                                   where c.UserId == user.Id && c.ClaimType == claim.Type && c.ClaimValue == claim.ValueType
                                   select c).FirstOrDefault(); 

            if(userClaim != null)
            {
                this.context.UserClaims.Remove(userClaim);
                await this.context.SaveChangesAsync(); 
            }
            else
            {
                throw new Exception("Cannot find requested claim"); 
            }

        }

        public async Task<string> GetPasswordHashAsync(TUser user)
        {
            User dbUser = this.context.Users.Where(u => u.Id == user.Id).FirstOrDefault<User>(); 

            if(dbUser != null && !String.IsNullOrEmpty(dbUser.Password))
            {
                return await Task.FromResult<string>(dbUser.Password);
            }

            return await Task.FromResult<string>(null);

        }

        public async Task<bool> HasPasswordAsync(TUser user)
        {
            bool hasPassword = !String.IsNullOrEmpty(user.Password);

            return await Task.FromResult<bool>(hasPassword);
        }
        

        public Task SetPasswordHashAsync(TUser user, string passwordHash)
        {
            if(user == null)
            {
                throw new ArgumentException("user"); 
            }

            user.Password = passwordHash;

            return Task.FromResult<Object>(null);
        }

        public Task<string> GetSecurityStampAsync(TUser user)
        {
            return Task.FromResult(user.SecurityStamp);
        }

        public Task SetSecurityStampAsync(TUser user, string stamp)
        {
            user.SecurityStamp = stamp;

            return Task.FromResult(0);
        }

        public async Task AddToRoleAsync(TUser user, string roleName)
        {
            Role role = this.context.Roles.Where(r => r.Name == roleName).FirstOrDefault<Role>();

            User dbUser = this.context.Users.Where(u => u.Id == user.Id).FirstOrDefault<User>();

            if (role != null && dbUser != null)
            {
                role.Users.Add(dbUser);
                await this.context.SaveChangesAsync(); 
            }
            else
            {
                throw new Exception("Cannot find user of role"); 
            }

            await Task.FromResult<Object>(null);
        }

        public async Task<IList<string>> GetRolesAsync(TUser user)
        {
            User dbUser = this.context.Users.Where(u => u.Id == user.Id).FirstOrDefault<User>();
            List<string> roleNames = new List<string>(); 

            if(dbUser != null && dbUser.Roles != null)
            {
                foreach(Role role in dbUser.Roles)
                {
                    roleNames.Add(role.Name); 
                } 
            }

           return await Task.FromResult<IList<string>>(roleNames);
        }

        public async Task<bool> IsInRoleAsync(TUser user, string roleName)
        {
            User dbUser = this.context.Users.Where(u => u.Id == user.Id).FirstOrDefault<User>(); 

            if(dbUser != null && dbUser.Roles != null)
            {
                foreach(Role role in dbUser.Roles)
                {
                    if (role.Name.Equals(roleName))
                    {
                        return await Task.FromResult<bool>(true);
                    }
                }
   
            }
            return await Task.FromResult<bool>(false);
        }

        public async Task RemoveFromRoleAsync(TUser user, string roleName)
        {
            User dbUser = this.context.Users.Where(u => u.Id == user.Id).FirstOrDefault<User>();

            if (dbUser != null && dbUser.Roles != null)
            {
                foreach (Role role in dbUser.Roles)
                {
                    if (role.Name.Equals(roleName))
                    {
                        dbUser.Roles.Remove(role); 
                    }
                }

                await this.context.SaveChangesAsync(); 
            }     
        }
           

        public async Task<DateTimeOffset> GetLockoutEndDateAsync(TUser user)
        {
            User dbUser = this.context.Users.Where(u => u.Id == user.Id).FirstOrDefault<User>();
            DateTimeOffset dateTimeOffset = new DateTimeOffset();

            if (dbUser != null && dbUser.LockoutEndDateUtc != null)
            {
                dateTimeOffset = new DateTimeOffset(dbUser.LockoutEndDateUtc.Value);
            }
        
            return await Task.FromResult<DateTimeOffset>(dateTimeOffset); 
        }

        public async Task SetLockoutEndDateAsync(TUser user, DateTimeOffset lockoutEnd)
        {
            User dbUser = this.context.Users.Where(u => u.Id == user.Id).FirstOrDefault<User>();   

            if (dbUser != null)
            {
                dbUser.LockoutEndDateUtc = lockoutEnd.Date;
                await this.context.SaveChangesAsync(); 
            }
        }

        public async Task<int> IncrementAccessFailedCountAsync(TUser user)
        {
            User dbUser = this.context.Users.Where(u => u.Id == user.Id).FirstOrDefault<User>();

            if (dbUser != null)
            {
                dbUser.AccessFailedCount++; 
                await this.context.SaveChangesAsync();
            }
            return await Task.FromResult<int>(dbUser.AccessFailedCount); 
        }

        public async Task ResetAccessFailedCountAsync(TUser user)
        {
            User dbUser = this.context.Users.Where(u => u.Id == user.Id).FirstOrDefault<User>();

            if (dbUser != null)
            {
                dbUser.AccessFailedCount = 0; 
                await this.context.SaveChangesAsync();
            }
        }

        public async Task<int> GetAccessFailedCountAsync(TUser user)
        {
            User dbUser = this.context.Users.Where(u => u.Id == user.Id).FirstOrDefault<User>();

            if (dbUser != null)
            {
                return await Task.FromResult<int>(dbUser.AccessFailedCount);
            }

            return await Task.FromResult<int>(0);
        }
  
        public async Task<bool> GetLockoutEnabledAsync(TUser user)
        {
            User dbUser = this.context.Users.Where(u => u.Id == user.Id).FirstOrDefault<User>();

            if (dbUser != null)
            {
                return await Task.FromResult<bool>(dbUser.LockoutEnabled);
            }

            return await Task.FromResult<bool>(true);
        }

        public async Task SetLockoutEnabledAsync(TUser user, bool enabled)
        {
            User dbUser = this.context.Users.Where(u => u.Id == user.Id).FirstOrDefault<User>();

            if (dbUser != null)
            {
                dbUser.LockoutEnabled = enabled; 
                await this.context.SaveChangesAsync();
            }
        }

        public void Dispose()
        {
            if(this.context != null)
            {
                this.context.Dispose();
                this.context = null; 
            }
        }

        public async Task SetTwoFactorEnabledAsync(TUser user, bool enabled)
        {
            User dbUser = this.context.Users.Where(u => u.Id == user.Id).FirstOrDefault<User>();

            if (dbUser != null)
            {
                dbUser.TwoFactorEnabled = enabled;
                await this.context.SaveChangesAsync();
            }

        }

        public async Task<bool> GetTwoFactorEnabledAsync(TUser user)
        {
            User dbUser = this.context.Users.Where(u => u.Id == user.Id).FirstOrDefault<User>();

            if (dbUser != null)
            {
                return await Task.FromResult(dbUser.TwoFactorEnabled);
            }
            else
            {
                throw new ArgumentException("User does not exists."); 
            }

        }

        #region IUserPhoneNumberStore
        public async Task SetPhoneNumberAsync(TUser user, string phoneNumber)
        {
            User dbUser = this.context.Users.Where(u => u.Id == user.Id).FirstOrDefault<User>();

            if (dbUser != null)
            {
                dbUser.PhoneNumber = phoneNumber;
                await this.context.SaveChangesAsync();
            }
        }

        public async Task<string> GetPhoneNumberAsync(TUser user)
        {
            User dbUser = this.context.Users.Where(u => u.Id == user.Id).FirstOrDefault<User>();

            if (dbUser != null)
            {
                return await Task.FromResult(dbUser.PhoneNumber);
            }
            else
            {
                throw new ArgumentException("User does not exists.");
            }
        }

        public async Task<bool> GetPhoneNumberConfirmedAsync(TUser user)
        {
            User dbUser = this.context.Users.Where(u => u.Id == user.Id).FirstOrDefault<User>();

            if (dbUser != null)
            {
                return await Task.FromResult(dbUser.PhoneNumberConfirmed);
            }
            else
            {
                throw new ArgumentException("User does not exists.");
            }
        }

        public async Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed)
        {
            User dbUser = this.context.Users.Where(u => u.Id == user.Id).FirstOrDefault<User>();

            if (dbUser != null)
            {
                dbUser.PhoneNumberConfirmed = confirmed;
                await this.context.SaveChangesAsync();
            }
        }
        #endregion

    }


}