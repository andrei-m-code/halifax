using Halifax.Api;
using Halifax.Excel;
using PeggysCove.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHalifax(halifax => halifax
    .AddSettings<AppSettings>()
    .ConfigureAuthentication(TokenHelper.JwtSecret, false, false, false));

var jwt = TokenHelper.CreateToken();
L.Info(jwt);

var app = builder.Build();
app.UseHalifax();
app.Run("http://*:5000");


// Excel Lib tests
// var people = new List<Person>
// {
//     new("John Smith", 34, "ABC Co"),
//     new("John Doe", 40, "XYZ Company")
// };
//
// var excel = new ExcelConverter<Person>();
// excel.AddMapping("Full Name", p => p.Name);
// excel.AddMapping("Age", p => p.Age);
//
// using var memoryCsv = new MemoryStream();
// using var memoryExcel = new MemoryStream();
// await excel.WriteExcelAsync(memoryExcel, people);
// await excel.WriteCsvAsync(memoryCsv, people);
//
// var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
// var filenameExcel = Path.Combine(desktop, "Export.xlsx");
// var filenameCsv = Path.Combine(desktop, "Export.csv");
//
//
// await File.WriteAllBytesAsync(filenameExcel, memoryExcel.ToArray());
// await File.WriteAllBytesAsync(filenameCsv, memoryCsv.ToArray());
//
// Console.Out.WriteLine("DONE!");
//
// record Person(string Name, int Age, string Company);