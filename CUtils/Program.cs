
namespace CUtils
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Start CUtils");
            if (args.Length == 0 && false) 
            {
                Console.WriteLine("usage: CUtils command filein fileout [options]");
                Console.WriteLine("  filein - input; fileout - output;");
                Console.WriteLine("  command:");
                Console.WriteLine("    -fog");
                return;
            }
            //string command = args[0], filein = args[1], fileout = args[2];
            string command = "-fog",
                filein = "C:\\Home\\FactographProjects\\syp_cassettes\\SypCassete\\meta\\SypCassete_current.fog", 
                fileout = "fogout.fog";
            command = "-last";

            if (command == "-fog")
            {
                var fog = new Factograph.Docs.OneFog(filein);
            }
            else if (command == "-last")
            {
                int count = 0;
                string[] lines = new string[8];
                System.IO.TextReader reader = new System.IO.StreamReader(filein);
                string? line = "";
                while ((line = reader.ReadLine()) != null) { lines[count % 8] = line; count++; }
                Console.WriteLine($"count={count}");
                for (int i = 0; i < 8; i++)
                {
                    Console.WriteLine(lines[(count + i) % 8]);
                }
                reader.Close();
            }
            else if (command == "")
            {

            }
            else
            {
                Console.WriteLine("Err: no available command");
            }


        }
    }
}
