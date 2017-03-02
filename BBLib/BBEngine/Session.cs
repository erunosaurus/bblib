using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using APICorrelationID = Bloomberglp.Blpapi.CorrelationID;
using APIEvent = Bloomberglp.Blpapi.Event;
using APIEventHandler = Bloomberglp.Blpapi.EventHandler;
using APIElement = Bloomberglp.Blpapi.Element;
using APIMessage = Bloomberglp.Blpapi.Message;
using APIName = Bloomberglp.Blpapi.Name;
using APIRequest = Bloomberglp.Blpapi.Request;
using APIService = Bloomberglp.Blpapi.Service;
using APISession = Bloomberglp.Blpapi.Session;
using APISessionOptions = Bloomberglp.Blpapi.SessionOptions;
//using APIInvalidRequestException = Bloomberglp.Blpapi.InvalidRequestException;

namespace BBLib.BBEngine
{
    /// <summary>
    /// Designs a session as a workspace where any type of Blpapi request/reponse or subscription paradigms can be made.
    /// </summary>
    public class Session
    {
        public override string ToString() { return thisSessionID.ToString(); }

        // Session server address
        private readonly string THIS_SERVER_HOST;
        private readonly int THIS_SERVER_PORT;

        // Session
        internal APISession thisAPISession;
        private readonly int thisSessionID;
        private ManualResetEventSlim thisAPISessionStarted = new ManualResetEventSlim(false);

        // API Services
        private readonly Object servicesLocker = new Object();
        internal Dictionary<string, Service> services = new Dictionary<string, Service>
            {
                { Referential.APIAUTH_SVC, new Service(new ManualResetEventSlim(false)) },
                { Referential.APIFLDS_SVC, new Service(new ManualResetEventSlim(false)) },
                { Referential.MKTDATA_SVC, new Service(new ManualResetEventSlim(false)) },
                { Referential.MKTBAR_SVC, new Service(new ManualResetEventSlim(false)) },
                { Referential.MKTVWAP_SVC, new Service(new ManualResetEventSlim(false)) },
                { Referential.PAGEDATA_SVC, new Service(new ManualResetEventSlim(false)) },
                { Referential.REFDATA_SVC, new Service(new ManualResetEventSlim(false)) },
                { Referential.TASVC_SVC, new Service(new ManualResetEventSlim(false)) }
            };

        // References correlation ID
        internal long lastControlReferenceID = 0;
        internal readonly Object controlReferenceLocker = new Object();
        internal Dictionary<BBEngine.CorrelationID, BBControl.ReferenceRequester> references = new Dictionary<BBEngine.CorrelationID, BBControl.ReferenceRequester>();

        // Subscriptions correlation ID
        internal long lastControlSubscriptionID = 0;
        internal readonly Object controlSubscriptionLocker = new Object();
        internal Dictionary<BBEngine.CorrelationID, BBControl.SubscriptionController> subscriptions = new Dictionary<BBEngine.CorrelationID, BBControl.SubscriptionController>();

        // Subscriptions controller ID
        internal long lastSubscriptionControllerID = 0;
        internal readonly Object subscriptionControllerLocker = new Object();
        internal Dictionary<long, BBControl.SubscriptionController> subscriptionControllers = new Dictionary<long, BBControl.SubscriptionController>();

        /// <summary>
        /// <c>Session</c> constructor.
        /// </summary>
        public Session()
            : this (Referential.SERVER_HOST, Referential.SERVER_PORT) { }

        /// <summary>
        /// <c>Session</c> constructor.
        /// </summary>
        /// <param name="ip">IP address of the server to connect to.</param>
        /// <param name="port">Server port.</param>
        public Session(string ip, int port)
        {
            // Server address
            THIS_SERVER_HOST = ip;
            THIS_SERVER_PORT = port;

            // Add new session to sessions list and create/start Blpapi session asynchronously
            thisSessionID = Interlocked.Increment(ref Referential.lastSessionID);
            Referential.sessions.Add(this, false);
            SessionStart();
        }

        /// <summary>
        /// <c>Session</c> finalizer.
        /// </summary>
        ~Session()
        {
            // Remove session of sessions list and close
            Referential.sessions.Remove(this);
            if (thisAPISessionStarted.Wait(0))
                thisAPISession.Stop(APISession.StopOption.ASYNC);
            // Session start problem, what happen if bloomberg connection shut down ????
        }

        /// <summary>
        /// Starts asynchronously session.
        /// </summary>
        public async void SessionStart()
        {
            if (!thisAPISessionStarted.Wait(0))
            {
                await Task.Run(() =>
                {
                    // Transport connection
                    APISessionOptions sessionOptions = new APISessionOptions();
                    sessionOptions.ServerHost = THIS_SERVER_HOST;
                    sessionOptions.ServerPort = THIS_SERVER_PORT;
                    System.Console.WriteLine("BBEngine.Session[" + this + "] << Connecting to " + THIS_SERVER_HOST + ":" + THIS_SERVER_PORT);

                    // Allow multiple subscription correlation IDs per message
                    sessionOptions.AllowMultipleCorrelatorsPerMsg = true;

                    // Create and start session
                    thisAPISession = new APISession(sessionOptions, new APIEventHandler(ProcessEvent));
                    thisAPISession.StartAsync();

                    // Wait for events
                    Object locker = new Object();
                    lock (locker)
                    {
                        System.Threading.Monitor.Wait(locker);
                    }
                }
                );
            }
        }

        /// <summary>
        /// Process asynchronously Blpapi return events by type.
        /// </summary>
        /// <param name="eventObj">Received event.</param>
        /// <param name="session">Blpapi session.</param>
        private void ProcessEvent(APIEvent eventObj, APISession session)
        {
            switch (eventObj.Type)
            {
                case APIEvent.EventType.SESSION_STATUS:
                    {
                        foreach (APIMessage message in eventObj.GetMessages())
                        {
                            if (message.MessageType.Equals("SessionStarted"))
                            {
                                thisAPISessionStarted.Set();
                                Referential.sessions[this] = true;
                                System.Console.WriteLine("BBEngine.Session[" + this + "] << Session started");
                                return;
                            }
                            else if (message.MessageType.Equals("SessionStartupFailure"))
                            {
                                System.Console.Error.WriteLine("BBEngine.Session[" + this + "] << Session startup failure");
                                thisAPISessionStarted.Reset();
                                Referential.sessions[this] = false;
                                return;
                            }
                            else if (message.MessageType.Equals("SessionTerminated"))
                            {
                                System.Console.WriteLine("BBEngine.Session[" + this + "] << Session terminated");
                                thisAPISessionStarted.Reset();
                                Referential.sessions[this] = false;
                                return;
                            }
                        }
                        break;
                    }
                case APIEvent.EventType.SERVICE_STATUS:
                    {
                        foreach (APIMessage message in eventObj.GetMessages())
                        {
                            foreach (string service in services.Keys)
                            {
                                if (service == (string)message.CorrelationID.Object)
                                {
                                    if (message.MessageType.Equals("ServiceOpened"))
                                    {
                                        System.Console.WriteLine("BBEngine.Session[" + this + "] << " + service + " successfully opened");
                                        services[service].Reference = thisAPISession.GetService(service);
                                        services[service].Status.Set();
                                    }
                                    else
                                    {
                                        System.Console.Error.Write("BBEngine.Session[" + this + "] << Unexpected 'SERVICE_STATUS' message: ");
                                        try
                                        {
                                            message.Print(System.Console.Error);
                                        }
                                        catch (Exception e)
                                        {
                                            System.Console.Error.WriteLine(e);
                                        }
                                    }
                                    return;
                                }
                            }
                        }
                        break;
                    }
                case APIEvent.EventType.PARTIAL_RESPONSE:
                    {
                        // List of failed correlation IDs (on which 'FetchData' method failed) to release
                        List<BBEngine.CorrelationID> correlationIDs = new List<BBEngine.CorrelationID>();

                        foreach (APIMessage message in eventObj.GetMessages())
                        {
                            BBEngine.CorrelationID correlationID = (BBEngine.CorrelationID)message.CorrelationID.Object;
                            try
                            {
                                if (!correlationIDs.Contains(correlationID))
                                    references[correlationID].FetchData(message);
                            }
                            catch
                            {
                                if (!correlationIDs.Contains(correlationID))
                                    correlationIDs.Add(correlationID);
                            }
                        }

                        foreach (BBEngine.CorrelationID item in correlationIDs)
                            references[item].responseStatus.Set();
                        break;
                    }
                case APIEvent.EventType.RESPONSE:
                    {
                        // List of correlation IDs to release
                        List<BBEngine.CorrelationID> correlationIDs = new List<BBEngine.CorrelationID>();

                        foreach (APIMessage message in eventObj.GetMessages())
                        {
                            BBEngine.CorrelationID correlationID = (BBEngine.CorrelationID)message.CorrelationID.Object;
                            try
                            {
                                references[correlationID].FetchData(message);
                            }
                            catch { }
                            finally
                            {
                                if (!correlationIDs.Contains(correlationID))
                                    correlationIDs.Add(correlationID);
                            }
                        }

                        foreach (BBEngine.CorrelationID item in correlationIDs)
                            references[item].responseStatus.Set();
                        break;
                    }
                case APIEvent.EventType.SUBSCRIPTION_STATUS:
                    {
                        foreach (APIMessage message in eventObj.GetMessages())
                        {
                            foreach (APICorrelationID item in message.CorrelationIDs)
                            {
                                BBEngine.CorrelationID correlationID = (BBEngine.CorrelationID)item.Object;

                                if (message.MessageType.Equals("SubscriptionStarted"))
                                {
                                    System.Console.WriteLine("BBEngine.Session[" + this + "][" + this.subscriptions[correlationID].ID + ":" + correlationID + "].Security[" + this.subscriptions[correlationID].GetSubscriptionByID(correlationID).ticker + "] << Subscription started");
                                    return;
                                }
                                else if (message.MessageType.Equals("SubscriptionFailure"))
                                {
                                    BBEngine.BBError bbError = new BBEngine.BBError(message.GetElement(BBEngine.Referential.REASON));
                                    System.Console.Error.WriteLine("BBEngine.Session[" + this + "][" + this.subscriptions[correlationID].ID + ":" + correlationID + "].Security[" + this.subscriptions[correlationID].GetSubscriptionByID(correlationID).ticker + "] << Subscription failure: " + bbError.Dump());
                                    
                                    // Remove subscription
                                    if (bbError.Code == "2")
                                    {
                                        this.subscriptions[correlationID].subscriptions.Remove(this.subscriptions[correlationID].GetSubscriptionByID(correlationID).ticker);
                                    }

                                    return;
                                }
                                else if (message.MessageType.Equals("SessionTerminated"))
                                {
                                    System.Console.WriteLine("BBEngine.Session[" + this + "][" + this.subscriptions[correlationID].ID + ":" + correlationID + "].Security[" + this.subscriptions[correlationID].GetSubscriptionByID(correlationID).ticker + "] << Subscription terminated");
                                    return;
                                }
                            }
                        }
                        break;
                    }
                case APIEvent.EventType.SUBSCRIPTION_DATA:
                    {
                        foreach (APIMessage message in eventObj.GetMessages())
                        {
                            foreach (APICorrelationID item in message.CorrelationIDs)
                            {
                                BBEngine.CorrelationID correlationID = (BBEngine.CorrelationID)item.Object;
                                subscriptions[correlationID].FetchData(message, correlationID);
                            }
                        }
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        /// <summary>
        /// Gets a Blpapi service reference.
        /// </summary>
        /// <param name="service">Service name to reference.</param>
        /// <returns>Indicates if the service was successfully (<c>true</c>) opened or not (<c>false</c>).</returns>
        private bool GetService(string service)
        {
            thisAPISessionStarted.Wait();
            lock (servicesLocker)
            {
                if (!services[service].Status.Wait(0))
                {
                    try
                    {
                        switch (service)
                        {
                            case Referential.MKTDATA_SVC:
                                {
                                    thisAPISession.OpenServiceAsync(service, new APICorrelationID(service));
                                    break;
                                }
                            case Referential.REFDATA_SVC:
                                {
                                    thisAPISession.OpenServiceAsync(service, new APICorrelationID(service));
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }
                    }
                    catch (Exception e)
                    {
                        System.Console.Error.WriteLine("BBEngine.Session[" + this + "] << Could not open " + service + ": " + e);
                        return false;
                    }
                    services[service].Status.Wait();
                }
                return true;
            }
        }

        /// <summary>
        /// Creates an <c>HistoricalData</c> requester instance (using same instance of a requester in multiple threads can raise conflicts).
        /// </summary>
        /// <returns>Returns an instance of an <c>HistoricalData</c> requester.</returns>
        public BBControl.ReferenceRequester HistoricalDataRequest()
        {
            // Open service
            if (!GetService(Referential.REFDATA_SVC))
                return null;

            // Set reference correlation ID
            lock (controlReferenceLocker)
            {
                BBEngine.CorrelationID correlationID;
                do
                {
                    correlationID = new BBEngine.CorrelationID(BBEngine.ControlTypes.Reference, ++lastControlReferenceID);
                } while (references.ContainsKey(correlationID));

                // Return requester instance
                BBControl.ReferenceRequester reference = new BBControl.ReferenceRequester.HistoricalData(this, correlationID);
                references.Add(correlationID, reference);
                return reference;
            }
        }

        /// <summary>
        /// Creates an <c>IntradayBar</c> requester instance (using same instance of a requester in multiple threads can raise conflicts).
        /// </summary>
        /// <returns>Returns an instance of an <c>IntradayBar</c> requester.</returns>
        public BBControl.ReferenceRequester IntradayBarRequest()
        {
            // Open service
            if (!GetService(Referential.REFDATA_SVC))
                return null;

            // Set reference correlation ID
            lock (controlReferenceLocker)
            {
                BBEngine.CorrelationID correlationID;
                do
                {
                    correlationID = new BBEngine.CorrelationID(BBEngine.ControlTypes.Reference, ++lastControlReferenceID);
                } while (references.ContainsKey(correlationID));

                // Return requester instance
                BBControl.ReferenceRequester reference = new BBControl.ReferenceRequester.IntradayBar(this, correlationID);
                references.Add(correlationID, reference);
                return reference;
            }
        }

        /// <summary>
        /// Creates an <c>IntradayTick</c> requester instance (using same instance of a requester in multiple threads can raise conflicts).
        /// </summary>
        /// <returns>Returns an instance of an <c>IntradayTick</c> requester.</returns>
        public BBControl.ReferenceRequester IntradayTickRequest()
        {
            // Open service
            if (!GetService(Referential.REFDATA_SVC))
                return null;

            // Set reference correlation ID
            lock (controlReferenceLocker)
            {
                BBEngine.CorrelationID correlationID;
                do
                {
                    correlationID = new BBEngine.CorrelationID(BBEngine.ControlTypes.Reference, ++lastControlReferenceID);
                } while (references.ContainsKey(correlationID));

                // Return requester instance
                BBControl.ReferenceRequester reference = new BBControl.ReferenceRequester.IntradayTick(this, correlationID);
                references.Add(correlationID, reference);
                return reference;
            }
        }

        /// <summary>
        /// Creates a <c>ReferenceData</c> requester instance (using same instance of a requester in multiple threads can raise conflicts).
        /// </summary>
        /// <returns>Returns an instance of a <c>ReferenceData</c> requester.</returns>
        public BBControl.ReferenceRequester ReferenceDataRequest()
        {
            // Open service
            if (!GetService(Referential.REFDATA_SVC))
                return null;

            // Set reference correlation ID
            lock (controlReferenceLocker)
            {
                BBEngine.CorrelationID correlationID;
                do
                {
                    correlationID = new BBEngine.CorrelationID(BBEngine.ControlTypes.Reference, ++lastControlReferenceID);
                } while (references.ContainsKey(correlationID));

                // Return requester instance
                BBControl.ReferenceRequester reference = new BBControl.ReferenceRequester.ReferenceData(this, correlationID);
                references.Add(correlationID, reference);
                return reference;
            }
        }

        public BBControl.SubscriptionController SubscriptionController()
        {
            // Open service
            if (!GetService(Referential.MKTDATA_SVC))
                return null;

            // Set subscription controller ID
            lock (subscriptionControllerLocker)
            {
                long ID;
                do
                {
                    ID = ++lastSubscriptionControllerID;
                } while (subscriptionControllers.ContainsKey(ID));

                // Return controller instance
                BBControl.SubscriptionController subscription = new BBControl.SubscriptionController(this, ID);
                subscriptionControllers.Add(ID, subscription);
                return subscription;
            }
        }
    }
}