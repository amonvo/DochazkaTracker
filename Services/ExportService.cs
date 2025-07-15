using OfficeOpenXml;

namespace DochazkaTracker.Services
{
    public class ExportService
    {
        public static async Task<string> ExportToExcelAsync(List<Dochazka> dochazky)
        {
            return await Task.Run(() => ExportToExcel(dochazky));
        }

        private static string ExportToExcel(List<Dochazka> dochazky)
        {
            string filePath = "dochazka.xlsx";

            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Docházka");

                // Hlavičky
                worksheet.Cells[1, 1].Value = "Datum";
                worksheet.Cells[1, 2].Value = "Příchod";
                worksheet.Cells[1, 3].Value = "Odchod";
                worksheet.Cells[1, 4].Value = "Rozdíl";
                worksheet.Cells[1, 5].Value = "Poznámka";
                worksheet.Row(1).Style.Font.Bold = true;

                // Data
                int row = 2;
                foreach (var dochazka in dochazky)
                {
                    worksheet.Cells[row, 1].Value = dochazka.Prichod.ToShortDateString();
                    worksheet.Cells[row, 2].Value = dochazka.Prichod.ToString("HH:mm");
                    worksheet.Cells[row, 3].Value = dochazka.Odchod?.ToString("HH:mm");
                    worksheet.Cells[row, 4].Value = dochazka.FormatovanyRozdil;
                    worksheet.Cells[row, 5].Value = dochazka.Rezim;
                    row++;
                }

                File.WriteAllBytes(filePath, package.GetAsByteArray());
            }

            return filePath;
        }
    }
}
