using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestOCR.Main
{
	class Program
	{
		static void Main(string[] args)
		{
			

			Thread trd1 = new Thread(new TestEngine().screenShots);
			
			
			trd1.Start();
			trd1.Join();
			Thread trd = new Thread(new TestEngine().imageProcess);
			trd.Start();
			trd.Join();
			Thread tr2 = new Thread(new TestEngine().Process);
			tr2.Start();
			
           
			

			
			

			

			
		}
	}
}
