using OfficeOpenXml;

namespace DochazkaTracker.Services
{
    public class ImportService
    {
        public static List<Dochazka> ImportFromExcel(string filePath)
        {
            List<Dochazka> importedDochazky = new List<Dochazka>();

            using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    return importedDochazky;
                }

                int rowCount = worksheet.Dimension.Rows;

                if (rowCount < 2)
                {
                    return importedDochazky;
                }

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        string datumText = worksheet.Cells[row, 1].Text.Trim();

                        if (string.IsNullOrWhiteSpace(datumText))
                        {
                            continue;
                        }

                        if (datumText.Length <= 7 && datumText.Contains("/"))
                        {
                            continue;
                        }

                        if (!DateTime.TryParse(datumText, out DateTime datum))
                        {
                            continue;
                        }

                        string prichodText = worksheet.Cells[row, 2].Text;
                        string odchodText = worksheet.Cells[row, 3].Text;
                        string rezim = worksheet.Cells[row, 5].Text;

                        DateTime prichod = DateTime.ParseExact($"{datum:dd.MM.yyyy} {prichodText}", "dd.MM.yyyy HH:mm", null);
                        DateTime? odchod = string.IsNullOrWhiteSpace(odchodText)
                            ? (DateTime?)null
                            : DateTime.ParseExact($"{datum:dd.MM.yyyy} {odchodText}", "dd.MM.yyyy HH:mm", null);

                        Dochazka novaDochazka = new Dochazka
                        {
                            Prichod = prichod,
                            Odchod = odchod,
                            Rezim = rezim
                        };
                        novaDochazka.VypocetRozdilu();

                        importedDochazky.Add(novaDochazka);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }

            return importedDochazky;
        }
    }
}
