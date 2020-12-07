using System;
using System.IO;
using System.Drawing;
using System.Drawing.Printing;


namespace AssemblyPrintout
{
    public class Export
    {
        private Font printFont;
        private StreamReader streamToPDF;
        public void ToPDF(string path)
        {
            string sourcePath = path + ".txt";
            string destPath = path + ".pdf";
            try
            {
                streamToPDF = new StreamReader(sourcePath);
                try
                {
                    printFont = new Font("Consolas", 8);
                    PrintDocument pd = new PrintDocument();
                    pd.PrinterSettings.PrintToFile = true;
                    pd.PrinterSettings.PrintFileName = destPath;
                    pd.PrinterSettings.PrinterName = "Microsoft Print to PDF";
                    pd.PrintPage += new PrintPageEventHandler(this.PD_PrintPage);
                    pd.Print();
                }
                finally
                {
                    streamToPDF.Close();
                }
            }
            catch (Exception e)
            {
                Console.Write(e.Message);
            }
        }
        private void PD_PrintPage(object sender, PrintPageEventArgs ev)
        {
            float LinesPerPage;
            float YPos;
            int Count = 0;
            float leftMargin = ev.MarginBounds.Left;
            float topMargin = ev.MarginBounds.Top;
            string line = null;

            // Calculate the number of lines per page.
            LinesPerPage = ev.MarginBounds.Height / printFont.GetHeight(ev.Graphics);

            // Print each line of the file.
            while (Count < LinesPerPage && ((line = streamToPDF.ReadLine()) != null))
            {
                YPos = topMargin + (Count * printFont.GetHeight(ev.Graphics));
                ev.Graphics.DrawString(line, printFont, Brushes.Black, leftMargin, YPos, new StringFormat());
                Count++;
            }

            // If more lines exist, print another page.
            if (line != null)
                ev.HasMorePages = true;
            else
                ev.HasMorePages = false;
        }
        //private void InitializeComponent()
        //{
        //    this.components = new System.ComponentModel.Container();
        //    this.printButton = new System.Windows.Forms.Button();

        //    this.ClientSize = new System.Drawing.Size(504, 381);
        //    this.Text = "Print Example";

        //    printButton.ImageAlign =
        //       System.Drawing.ContentAlignment.MiddleLeft;
        //    printButton.Location = new System.Drawing.Point(32, 110);
        //    printButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        //    printButton.TabIndex = 0;
        //    printButton.Text = "Print the file.";
        //    printButton.Size = new System.Drawing.Size(136, 40);
        //    printButton.Click += new System.EventHandler(printButton_Click);

        //    this.Controls.Add(printButton);
        //}
    }
}
