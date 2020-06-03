using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenoSipFactory
{
	public class fs_audio
	{
		public static string audio_ringback
		{
			get { return "tone_stream://%(1000,4000,450)"; }
		}
	}
}
