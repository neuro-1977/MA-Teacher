using System.Net;
using System.Text.Json;
namespace MATeacher.ModuleShell;
internal static class ReleaseSelfTest
{
    public static async Task<int> RunAsync()
    {
        var ui=Path.Combine(AppContext.BaseDirectory,"ui");
        if(!File.Exists(Path.Combine(ui,"index.html"))) return 10;
        var data=Path.Combine(Path.GetTempPath(),$"ma-teacher-self-test-{Guid.NewGuid():N}");
        try
        {
            using var host=new LocalModuleHost(ui,data);
            if(!await host.StartAsync()) return 11;
            using var client=new HttpClient{Timeout=TimeSpan.FromSeconds(30)};
            var healthResponse=await client.GetAsync(new Uri(new Uri(host.BaseAddress),"api/health"));
            if(healthResponse.StatusCode!=HttpStatusCode.OK) return 12;
            using var health=JsonDocument.Parse(await healthResponse.Content.ReadAsStringAsync());
            if(!health.RootElement.TryGetProperty("ok",out var ok)||!ok.GetBoolean()) return 13;
            var root=await client.GetAsync(host.BaseAddress);
            if(root.StatusCode!=HttpStatusCode.OK||!(await root.Content.ReadAsStringAsync()).Contains("MA-Teacher",StringComparison.OrdinalIgnoreCase)) return 14;
            if((await client.GetAsync(new Uri(new Uri(host.BaseAddress),"assets/definitely-missing.js"))).StatusCode!=HttpStatusCode.NotFound) return 15;
            return 0;
        }
        catch{return 20;}
        finally{try{if(Directory.Exists(data))Directory.Delete(data,true);}catch{}}
    }
}
