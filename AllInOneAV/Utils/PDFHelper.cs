using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using iTextSharp.text;

namespace Utils
{
    public class PDFHelper
    {
        public static void CombinePicturesToPdf(List<string> pictures, string folder, string name)
        {
            float x = Image.GetInstance(pictures[0]).Width;
            float y = 14400;

            Rectangle pageSize = new Rectangle(x, y);
            iTextSharp.text.Document document = new iTextSharp.text.Document(pageSize, 0, 0, 0, 0);

            try
            {
                iTextSharp.text.pdf.PdfWriter.GetInstance(document, new FileStream(folder + @"\" + name + ".pdf", FileMode.Create, FileAccess.ReadWrite));
                document.Open();
                Image image;

                document.NewPage();

                for (int i = 0; i < pictures.Count; i++)
                {
                    if (string.IsNullOrEmpty(pictures[i])) break;

                    image = Image.GetInstance(pictures[i]);
                    image.Alignment = Image.ALIGN_MIDDLE;

                    if (y + image.Height > 14400)
                    {
                        y = 0;
                        document.NewPage();
                        document.Add(image);
                    }
                    else
                    {
                        y += image.Height;
                        document.Add(image);
                    }
                }

                Console.WriteLine("转换成功！");
            }
            catch (Exception ex)
            {
                Console.WriteLine("转换失败，原因：" + ex.Message);
            }

            document.Close();
        }
    }
}
