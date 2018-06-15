//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace AssemblyPrintout
//{
//    class Class1
//    {
//        using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.IO;

//namespace ConsoleApplication1
//    {
//        class Program
//        {
//            public static int pos = 0;
//            static void Main(string[] args)
//            {
//                readFromFile("D:\\student.txt");
//                addrecord();

//            }


//            class Student
//            {

//                private string stnumber;
//                private string stname;
//                private int recordsize;


//                public string stunumber
//                { //stunumber property

//                    set { stnumber = value; }
//                    get { return stnumber; }



//                }
//                public string stuname
//                { //stuname property

//                    set { stname = value; }
//                    get { return stname; }



//                }

//                public int size
//                {

//                    get { return calsize(); }
//                }
//                private int calsize()
//                {
//                    recordsize = 2 * 15 + 2 * 20;// max record size
//                    return recordsize;
//                }
//            }


//            static void addrecord()
//            {


//                Student stu = new Student();
//                String con = "y";
//                while (con != "n")
//                {

//                    Console.Write("Enter student number:");

//                    stu.stunumber = Console.ReadLine();

//                    Console.Write("Enter student name:");

//                    stu.stuname = Console.ReadLine();
//                    pos += 1; //update position
//                    writeToFile("D:\\Student.txt", stu, pos, stu.size);
//                    Console.WriteLine("Continue?y/n:");
//                    con = Console.ReadLine();
//                }




//            }

//            static void writeToFile(string filename, Student obj, int pos, int size)
//            {

//                FileStream fout;

//                BinaryWriter bw;

//                //create a file stream object

//                fout = new FileStream(filename, FileMode.Append, FileAccess.Write);

//                //create a binary writer object
//                bw = new BinaryWriter(fout);

//                //set file position where to write data
//                fout.Position = pos * size;
//                //write data
//                bw.Write(obj.stunumber);
//                bw.Write(obj.stuname);
//                //close objects
//                bw.Close();
//                fout.Close();



//            }

//            static void readFromFile(string filename)
//            {


//                FileStream fn;
//                BinaryReader br;

//                Student stu = new Student();

//                int currentrecord = 0;


//                //open file to read data

//                fn = new FileStream(filename, FileMode.Open, FileAccess.Read);


//                br = new BinaryReader(fn);

//                //read next record
//                int i;
//                for (i = 1; i <= (int)(fn.Length) / stu.size; i++)
//                {

//                    currentrecord += 1; //update currentrecord position

//                    fn.Seek(currentrecord * stu.size, 0);

//                    stu.stunumber = br.ReadString().ToString();
//                    stu.stuname = br.ReadString().ToString();

//                    Console.WriteLine(stu.stunumber + "\t" + stu.stuname);
//                }
//                //update pos to the current position
//                pos = currentrecord;
//                //close objects
//                br.Close();
//                fn.Close();
//            }
//        }
//    }
//}

