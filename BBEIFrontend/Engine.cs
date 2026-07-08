using BBEIDataAccess.Models;
using BBEILib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SendGrid;
using SendGrid.Helpers.Mail;
using Serilog;
using System.Reflection.Metadata;
using System.Text;
using System.Xml.Serialization;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
namespace BBEIFrontend
{
    public class Engine
    {
        public Engine() { }
        internal void AvvioMotore()
        {
            bool first = true;
            while (true)
            {
                try
                {
                    if (!first)
                        Program.EventEngine.WaitOne(10000);

                    Log.Information("Main Loop Enabled");
                    GestoreDb gestoreDb = new GestoreDb();


                    first = false;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }
        }
    }
}
