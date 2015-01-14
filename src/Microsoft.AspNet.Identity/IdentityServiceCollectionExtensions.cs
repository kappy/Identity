// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Security;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class IdentityServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureIdentity<TUser>(this IServiceCollection services, Action<IdentityOptions<TUser>> configure) where TUser : class
        {
            return services.Configure(configure);
        }

        public static IdentityBuilder AddIdentity(this IServiceCollection services)
        {
            return services.AddIdentity<IdentityUser, IdentityRole>();
        }

        public static IdentityBuilder AddIdentity(
            this IServiceCollection services, 
            IConfiguration identityConfig = null,
            Action<IdentityOptions<IdentityUser>> configureOptions = null,
            bool useDefaultSubKey = true)
        {
            return services.AddIdentity<IdentityUser, IdentityRole>(identityConfig, configureOptions, useDefaultSubKey);
        }

        public static IdentityBuilder AddIdentity<TUser, TRole>(
            this IServiceCollection services, 
            IConfiguration identityConfig = null, 
            Action<IdentityOptions<TUser>> configureOptions = null, 
            bool useDefaultSubKey = true)
            where TUser : class
            where TRole : class
        {
            if (identityConfig != null)
            {
                if (useDefaultSubKey)
                {
                    identityConfig = identityConfig.GetSubKey("identity");
                }
                services.Configure<IdentityOptions<TUser>>(identityConfig);
            }
            var describe = new ServiceDescriber(identityConfig);

            // Services used by identity
            services.AddOptions(identityConfig);
            services.AddDataProtection(identityConfig);

            // Identity services
            services.TryAdd(describe.Transient<IUserValidator<TUser>, UserValidator<TUser>>());
            services.TryAdd(describe.Transient<IPasswordValidator<TUser>, PasswordValidator<TUser>>());
            services.TryAdd(describe.Transient<IPasswordHasher<TUser>, PasswordHasher<TUser>>());
            services.TryAdd(describe.Transient<ILookupNormalizer, UpperInvariantLookupNormalizer>());
            services.TryAdd(describe.Transient<IRoleValidator<TRole>, RoleValidator<TRole>>());
            // No interface for the error describer so we can add errors without rev'ing the interface
            services.TryAdd(describe.Transient<IdentityErrorDescriber, IdentityErrorDescriber>());
            services.TryAdd(describe.Scoped<ISecurityStampValidator, SecurityStampValidator<TUser>>());
            services.TryAdd(describe.Scoped<IClaimsIdentityFactory<TUser>, ClaimsIdentityFactory<TUser, TRole>>());
            services.TryAdd(describe.Scoped<UserManager<TUser>, UserManager<TUser>>());
            services.TryAdd(describe.Scoped<SignInManager<TUser>, SignInManager<TUser>>());
            services.TryAdd(describe.Scoped<RoleManager<TRole>, RoleManager<TRole>>());

            if (configureOptions != null)
            {
                services.ConfigureIdentity(configureOptions);
            }
            services.Configure<ExternalAuthenticationOptions>(options =>
            {
                options.SignInAsAuthenticationType = IdentityAuthenticationTypes.ExternalCookieAuthenticationType;
            });

            // Configure all of the cookie middlewares
            services.Configure<CookieAuthenticationOptions>(options =>
            {
                options.AuthenticationType = IdentityAuthenticationTypes.ApplicationCookieAuthenticationType;
                options.LoginPath = new PathString("/Account/Login");
                options.Notifications = new CookieAuthenticationNotifications
                {
                    OnValidateIdentity = SecurityStampValidator.ValidateIdentityAsync<TUser>
                };
            }, IdentityAuthenticationTypes.ApplicationCookieAuthenticationType);
            services.Configure<CookieAuthenticationOptions>(options =>
            {
                options.AuthenticationType = IdentityAuthenticationTypes.ExternalCookieAuthenticationType;
                options.AuthenticationMode = AuthenticationMode.Passive;
                options.CookieName = IdentityAuthenticationTypes.ExternalCookieAuthenticationType;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            }, IdentityAuthenticationTypes.ExternalCookieAuthenticationType);
            services.Configure<CookieAuthenticationOptions>(options =>
            {
                options.AuthenticationType = IdentityAuthenticationTypes.TwoFactorRememberMeCookieAuthenticationType;
                options.AuthenticationMode = AuthenticationMode.Passive;
                options.CookieName = IdentityAuthenticationTypes.TwoFactorRememberMeCookieAuthenticationType;
            }, IdentityAuthenticationTypes.TwoFactorRememberMeCookieAuthenticationType);
            services.Configure<CookieAuthenticationOptions>(options =>
            {
                options.AuthenticationType = IdentityAuthenticationTypes.TwoFactorUserIdCookieAuthenticationType;
                options.AuthenticationMode = AuthenticationMode.Passive;
                options.CookieName = IdentityAuthenticationTypes.TwoFactorUserIdCookieAuthenticationType;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            }, IdentityAuthenticationTypes.TwoFactorUserIdCookieAuthenticationType);

            return new IdentityBuilder(typeof(TUser), typeof(TRole), services);
        }
    }
}