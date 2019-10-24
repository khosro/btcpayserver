using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServer.Configuration;
using BTCPayServer.Data;
using BTCPayServer.Logging;
using BTCPayServer.Models.AccountViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NETCore.Encrypt.Extensions.Internal;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using System.Linq;

namespace BTCPayServer
{
    public static class OpenIddictExtensions
    {
        public static SecurityKey GetSigningKey(IConfiguration configuration)
        {
          
            var file = Path.Combine(configuration.GetDataDir(), "rsaparams");
            var rsa = new RSACryptoServiceProvider(2048);
            if (File.Exists(file))
            {
                rsa.FromXmlString2(File.ReadAllText(file));
            }
            else
            {
                var contents = rsa.ToXmlString2(true);
                File.WriteAllText(file, contents);
            }
            return new RsaSecurityKey(rsa.ExportParameters(true));;
        }
        public static OpenIddictServerBuilder ConfigureSigningKey(this OpenIddictServerBuilder builder,
            IConfiguration configuration)
        {
            return builder.AddSigningKey(GetSigningKey(configuration));
        }
    }
    public class OpenIdStartupTask : IStartupTask
    {
        IServiceProvider _serviceProvide;
        public OpenIdStartupTask(IServiceProvider serviceProvide)
        {
            this._serviceProvide = serviceProvide;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            {
                try
                {
                    var openIddictApplicationManager = _serviceProvide.GetService<OpenIddictApplicationManager<BTCPayOpenIdClient>>();

                    if (!(await openIddictApplicationManager.ListAsync()).Any())
                    {

                        var id = Guid.NewGuid().ToString();
                        var passs = Guid.NewGuid().ToString();
                        var descriptor = new OpenIddictApplicationDescriptor()
                        {
                            ClientId = id,
                            DisplayName = id,
                            Permissions = { OpenIddictConstants.Permissions.GrantTypes.Password, OpenIddictConstants.Permissions.GrantTypes.RefreshToken, OpenIddictConstants.Permissions.GrantTypes.Implicit,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,OpenIddictConstants.Permissions.GrantTypes.ClientCredentials}
                        };
                        var RegisterDetails = new RegisterViewModel()
                        {
                            Email = id + "@idea.com",
                            ConfirmPassword = passs,
                            Password = passs
                        };
                        //await account.Register(RegisterDetails);
                        //var UserId = account.RegisteredUserId;
                        var _userManager = _serviceProvide.GetService<UserManager<ApplicationUser>>();
                        var user = new ApplicationUser { UserName = RegisterDetails.Email, Email = RegisterDetails.Email, RequiresEmailConfirmation = false };
                        var User = await _userManager.CreateAsync(user, RegisterDetails.Password);
                        var client = new BTCPayOpenIdClient { ApplicationUserId = user.Id };
                        await openIddictApplicationManager.PopulateAsync(client, descriptor);
                        await openIddictApplicationManager.CreateAsync(client, "secret");
                    }
                }
                catch (Exception ex)
                {
                    Logs.PayServer.LogError(ex, "Error on the MigrationStartupTask");
                    throw;
                };
            }
        }
    }
}
