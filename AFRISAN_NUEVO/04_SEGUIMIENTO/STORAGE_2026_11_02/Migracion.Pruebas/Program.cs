using System.Text.Json;
using Afrisan.Nuevo.Api;
using Afrisan.Nuevo.Api.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Npgsql;

// Arnés aislado: nunca carga ni escribe User Secrets reales.
var config=new ConfigurationBuilder().AddEnvironmentVariables().Build();
var parsed=new NpgsqlConnectionStringBuilder(config.GetConnectionString("AfrisanRuntime"));
if(parsed.Host!="127.0.0.1"||parsed.Port!=55432)throw new InvalidOperationException("Las pruebas solo admiten la base local aislada.");
var env=new EntornoPruebas();
var options=PersistenciaOptions.Load(config,env);
var factory=new PooledDbContextFactory<AfrisanDb>(new DbContextOptionsBuilder<AfrisanDb>().UseNpgsql(options.ConnectionString).Options);
var database=new PgDatabase(factory);await options.Inicializar(database);
var storageOptions=StorageOptions.Load(config,env);
using var handler=new HttpClientHandler{AllowAutoRedirect=false};using var http=new HttpClient(handler);
var fotos=new FotosPrivadas(options,storageOptions,new SupabaseFotos(http,storageOptions));
var saved=new Dictionary<string,string>();
if(JsonSerializer.Deserialize<FotoArchivo>("{}",new JsonSerializerOptions(JsonSerializerDefaults.Web))!.Bucket!="servidor-privado")throw new InvalidOperationException("Incompatibilidad con metadatos históricos sin Bucket.");
try {
    await ConfigurarStorage.Ejecutar(config,env,database,fotos,(section,name,value)=>saved[section+":"+name]=value);
    await File.WriteAllTextAsync(config["AFRISAN_TEST_CONFIG_FILE"]!,JsonSerializer.Serialize(saved));
}catch(Exception ex){Console.Error.WriteLine(ex is ReglaInventarioException or InvalidOperationException?ex.Message:"Error en migración de prueba.");Environment.ExitCode=1;}

sealed class EntornoPruebas:IHostEnvironment
{
 public string EnvironmentName{get;set;}="Development";
 public string ApplicationName{get;set;}="Pruebas.Storage";
 public string ContentRootPath{get;set;}=Directory.GetCurrentDirectory();
 public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();
}
