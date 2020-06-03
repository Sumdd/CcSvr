using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using CenoSipCore;
using System.Net.Sockets;
using CenoCommon;

namespace CenoSipBusiness
{
	public class OutboundServerInstance : OutboundServer
	{
		public OutboundServerInstance(IPEndPoint address)
			: base(address)
		{
		}
		public override void handle_request(Socket socket)
		{
			OutboundSocketInstance _socket = new OutboundSocketInstance(socket);
		}
	}

	public class OutboundSocketInstance : OutboundSocket
	{
		public OutboundSocketInstance(Socket sock)
			: base(sock)
		{
		}

		public override void run()
		{
			base.OnCHANNEL_EXECUTE_COMPLETE += new EventHandlers(on_channel_execute_complete);
			base.OnCHANNEL_HANGUP_COMPLETE += new EventHandlers(on_channel_hangup_complete);
			base.OnCHANNEL_ANSWER += new EventHandlers(on_channel_answer);
			base.OnCHANNEL_STATE += new EventHandlers(OutboundSocketInstance_OnCHANNEL_STATE);
			base.OnAPI += new EventHandlers(OutboundSocketInstance_OnAPI);
			base.OnALL += new EventHandlers(OutboundSocketInstance_OnALL);

			base.connect();
			//answer();
			//playback(@"say:tts_commandline:Ting-Ting:welcome to freeswitch");

			//playback(@"silence_stream://5000,1400");
			//playback(@"local_stream://moh");
			bgapi("version");
		}

		private void on_channel_execute_complete(Event ev)
		{
			if (ev.get_header("Application") == "play_and_get_digits")
			{
			}
		}

		private void on_channel_hangup_complete(Event evt)
		{
			//LogWrite.ShowTxt(string.Format("EventName:{0}", evt.get_header("Channel-Unique-ID")));
		}

		private void on_channel_answer(Event ev)
		{
			//LogWrite.ShowTxt(string.Format("EventName:{0}", ev.get_header("Channel-Unique-ID")));

			//LogWrite.ShowTxt("Call answered");
		}

		void OutboundSocketInstance_OnAPI(Event message)
		{
			//LogWrite.ShowTxt(string.Format("EventName:{0}", message.get_header("Core-UUID")));
		}

		void OutboundSocketInstance_OnCHANNEL_STATE(Event message)
		{
			//LogWrite.ShowTxt(string.Format("EventName:{0}", message.get_header("Core-UUID")));
		}

		void OutboundSocketInstance_OnALL(Event message)
		{
			//LogWrite.ShowTxt(string.Format("EventName:{0}", message.get_header("Core-UUID")));
		}
	}

}
