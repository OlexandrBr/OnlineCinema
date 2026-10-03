using OnlineCinema.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<MovieStorage>();

var app = builder.Build();
app.UseStaticFiles();
app.MapControllerRoute("default", "{controller=Movies}/{action=Index}/{id?}");
app.Run();
