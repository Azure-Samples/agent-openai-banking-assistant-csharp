public static class ServicesExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IAccountService, AccountService>();

        services.AddSingleton<IUserService, UserService>();

        return services;
    }
}
