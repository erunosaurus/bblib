using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using BBLib.BBEngine;
using BBLib.BBControl;

namespace Console
{
    class Program
    {
        // REFERENCE DATA EXAMPLE

        /*static void Main(string[] args)
        {
            // Create a session
            Session session = new BBLib.BBEngine.Session();

            // Create a reference data requester
            ReferenceRequester requester = session.ReferenceDataRequest();

            // Add securities: GDF Suez & Schneider Electric SA
            requester.AddSecurities("GSZ FP Equity", "SU FP Equity");

            // Add fields
            requester.AddFields("PX_LAST", "PX_VOLUME"); // Default return type: string
            requester.AddField("PX_TO_FREE_CASH_FLOW", typeof(double)); // Custom return type

            // Send request
            System.Data.DataSet result;
            if (requester.SendRequest(out result))
                // Write the result in console (xml format)
                System.Console.WriteLine(result.GetXml());

            System.Console.Read();
        }*/

        // HISTORICAL DATA EXAMPLE

        /*static void Main(string[] args)
        {
            // Create a session
            Session session = new BBLib.BBEngine.Session();

            // Create an historical data requester
            ReferenceRequester requester = session.HistoricalDataRequest();

            // Add securities: GDF Suez & Schneider Electric SA
            requester.AddSecurities("GSZ FP Equity", "SU FP Equity");

            // Add fields
            requester.AddFields("PX_LAST"); // Default return type: string
            requester.AddField("PX_VOLUME", typeof(long)); // Custom return type

            // Add parameters
            requester.AddParameter("startDate", "20130718");
            requester.AddParameter("endDate", "20130719");

            // Send request
            System.Data.DataSet result;
            if (requester.SendRequest(out result))
                // Write the result in console (xml format)
                System.Console.WriteLine(result.GetXml());

            System.Console.Read();
        }*/

        // INTRADAY TICK EXAMPLE

        /*static void Main(string[] args)
        {
            // Create a session
            Session session = new BBLib.BBEngine.Session();

            // Create an historical data requester
            ReferenceRequester requester = session.IntradayTickRequest();

            // Add securities: GDF Suez & Schneider Electric SA
            requester.AddSecurities("GSZ FP Equity", "SU FP Equity");

            // Add event types
            requester.AddEventTypes("TRADE");

            // Add parameters
            requester.AddParameter("startDateTime", "2013-07-19T10:45:00");
            requester.AddParameter("endDateTime", "2013-07-19T10:45:10");

            // Send request
            System.Data.DataSet result;
            if (requester.SendRequest(out result))
                // Write the result in console (xml format)
                System.Console.WriteLine(result.GetXml());

            System.Console.Read();
        }*/

        // INTRADAY TICK EXAMPLE

        /*static void Main(string[] args)
        {
            // Create a session
            Session session = new BBLib.BBEngine.Session();

            // Create an historical data requester
            ReferenceRequester requester = session.IntradayBarRequest();

            // Add securities: GDF Suez & Schneider Electric SA
            requester.AddSecurities("GSZ FP Equity", "SU FP Equity");

            // Add event types
            requester.AddEventTypes("TRADE");

            // Add parameters
            requester.AddParameter("startDateTime", "2013-07-19T10:45:00");
            requester.AddParameter("endDateTime", "2013-07-19T10:55:00");
            requester.AddParameter("interval", 300);

            // Send request
            System.Data.DataSet result;
            if (requester.SendRequest(out result))
                // Write the result in console (xml format)
                System.Console.WriteLine(result.GetXml());

            System.Console.Read();
        }*/

        // SUBSCRIPTION EXAMPLE

        static void Event_SubscriptionUpdate(object sender, BBLib.BBControl.SubscriptionEventArgs e)
        {
            if (e.Error == null)
                System.Console.WriteLine(
                    DateTime.Now.ToString() + ": "
                    + e.Ticker + ": " + e.Field + " = "
                    + e.NewValue);
            else
                System.Console.WriteLine(
                    DateTime.Now.ToString() + ": "
                    + e.Ticker + ": " + e.Field + " = "
                    + e.Error);
        }

        static void Main(string[] args)
        {
            // Create a session
            Session session = new BBLib.BBEngine.Session();

            // Create a subscription controller
            SubscriptionController controller = session.SubscriptionController();
            // Handle update event
            controller.SubscritionUpdate += Event_SubscriptionUpdate;

            // Create a subscription: GDF Suez
            Subscription subscription1 = new Subscription("GSZ FP Equity"); // Ticker
            subscription1.AddFields("LAST_PRICE", "VOLUME_TDY"); // Fields
            subscription1.AddParameter("interval", 2); // Optional (see documentation)

            // Create a subscription: Schneider Electric SA
            Subscription subscription2 = new Subscription("AAPL US Equity"); // Ticker
            subscription2.AddFields("LAST_PRICE", "VOLUME_TDY"); // Fields
            subscription2.AddParameter("interval", 2); // Optional (see documentation)

            // Add subscription
            controller.AddSubscriptions(subscription1, subscription2);

            System.Console.Read();
        }
    }
}
