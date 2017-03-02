using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using APICorrelationID = Bloomberglp.Blpapi.CorrelationID;
using APIElement = Bloomberglp.Blpapi.Element;
using APIMessage = Bloomberglp.Blpapi.Message;
using APISubscription = Bloomberglp.Blpapi.Subscription;

namespace BBLib.BBControl
{
    /// <summary>
    /// Designs subscription event arguments.
    /// </summary>
    public class SubscriptionEventArgs : EventArgs
    {
        // Data
        public readonly string Ticker;
        public readonly string Field;

        // Type
        private readonly System.Type type;

        // Value
        private readonly string oldValue;
        public dynamic OldValue
        {
            get
            {
                // Convert field value to 'Type' property
                MethodInfo method = typeof(BBEngine.Functions).GetMethod("GetValueAsConverted");
                method = method.MakeGenericMethod(this.type);

                try
                {
                    return method.Invoke(null, new object[] { this.oldValue });
                }
                catch
                {
                    return this.oldValue;
                }
            }
        }

        private readonly string newValue;
        public dynamic NewValue
        {
            get
            {
                // Convert field value to 'Type' property
                MethodInfo method = typeof(BBEngine.Functions).GetMethod("GetValueAsConverted");
                method = method.MakeGenericMethod(this.type);

                try
                {
                    return method.Invoke(null, new object[] { this.newValue });
                }
                catch
                {
                    return this.newValue;
                }
            }
        }

        // Error
        public readonly string Error;

        internal SubscriptionEventArgs(string ticker, string field, System.Type type, string oldValue, string newValue, string error)
        {
            this.Ticker = ticker;
            this.Field = field;
            this.type = type;
            this.oldValue = oldValue;
            this.newValue = newValue;
            this.Error = error;
        }
    }

    /// <summary>
    /// Designs a subscribed security.
    /// </summary>
    internal class SubscribedSecurity
    {
        // Subscription controller reference
        public readonly SubscriptionController controller;

        // Subscribed security
        public readonly string ticker;

        // Subscribed security correlation ID
        public BBEngine.CorrelationID correlationID;

        // Subscribed security fields
        public Dictionary<string, SubscribedField> fields = new Dictionary<string, SubscribedField>();
        public Dictionary<string, SubscribedField> Fields { get { return this.fields; } }

        // Subscribed security parameters
        public Dictionary<string, dynamic> parameters = new Dictionary<string, dynamic>();

        /// <summary>
        /// <c>SubscribedSecurity</c> constructor.
        /// </summary>
        /// <param name="controller">Reference of the subscription controller.</param>
        /// <param name="fields">Set of fields to subscribe to.</param>
        /// <param name="parameters">Set of subscription parameters.</param>
        public SubscribedSecurity(SubscriptionController controller, string ticker, Dictionary<string, System.Type> fields, Dictionary<string, dynamic> parameters)
        {
            // Set controller reference
            this.controller = controller;

            // Set security ticker
            this.ticker = ticker;

            // Set security fields
            foreach (string item in fields.Keys)
            {
                this.fields.Add(item, new SubscribedField(this, item, fields[item]));
            }

            // Set security parameters
            foreach (string item in parameters.Keys)
            {
                this.parameters.Add(item, parameters[item]);
            }

            // Set security correlation ID
            lock (controller.session.controlSubscriptionLocker)
            {
                BBEngine.CorrelationID correlationID;
                do
                {
                    correlationID = new BBEngine.CorrelationID(BBEngine.ControlTypes.Subscription, ++controller.session.lastControlSubscriptionID);
                } while (controller.session.subscriptions.ContainsKey(correlationID));

                this.correlationID = correlationID;
                controller.session.subscriptions.Add(correlationID, controller);
            }
        }

        /// <summary>
        /// <c>SubscribedSecurity</c> finalizer.
        /// </summary>
        ~SubscribedSecurity()
        {
            this.controller.session.subscriptions.Remove(this.correlationID);
        }

        /// <summary>
        /// Updates subscribed fields
        /// </summary>
        /// <param name="fields">Set of fields to subscribe to.</param>
        public void UpdateFields(Dictionary<string, System.Type> fields)
        {
            // Remove subscribed fields not provided by input fields
            foreach (string item in this.fields.Keys)
            {
                if (!fields.ContainsKey(item))
                    this.fields.Remove(item);
            }

            // Add/update subscribed fields provided by input fields
            foreach (string item in fields.Keys)
            {
                if (this.fields.ContainsKey(item))
                    this.fields[item].Type = fields[item];
                else
                    this.fields.Add(item, new SubscribedField(this, item, fields[item]));
            }
        }
    }

    /// <summary>
    /// Designs a subscribed field.
    /// </summary>
    internal class SubscribedField
    {
        // Security reference
        public readonly SubscribedSecurity security;

        // Field properties
        public readonly string ticker;
        public readonly string field;

        // Field type
        private System.Type type;
        public System.Type Type
        {
            get { return this.type; }
            set { this.type = value; }
        }

        // Field value
        private string value = null;
        /// <summary>
        /// Get/set the subscribed field value
        /// </summary>
        public string Value
        {
            get { return this.value; }
            set
            {
                // Update field value
                string oldValue = this.Value;
                this.value = value;
                this.error = null;
                string newValue = this.Value;

                if (oldValue == newValue)
                    return;
                else
                {
                    security.controller.OnUpdate(this, new SubscriptionEventArgs(this.ticker, this.field, this.Type, oldValue, newValue, this.error));
                }
            }
        }

        // Field error
        private string error = null;
        /// <summary>
        /// Get/set the subscribed field error
        /// </summary>
        public string Error
        {
            get
            {
                return error;
            }
            set
            {
                // Update field error
                this.error = value;
                security.controller.OnUpdate(this, new SubscriptionEventArgs(this.ticker, this.field, this.Type, this.Value, this.Value, this.error));
            }
        }

        /// <summary>
        /// <c>SubscribedField</c> constructor.
        /// </summary>
        /// <param name="security">Reference of the security.</param>
        /// <param name="type">Field type.</param>
        public SubscribedField(SubscribedSecurity security, string field, System.Type Type)
        {
            // Set security reference
            this.security = security;

            // Set field properties
            this.ticker = security.ticker;
            this.field = field;

            // Set field type
            this.type = Type;
        }
    }

    /// <summary>
    /// Designs a subscription controller.
    /// </summary>
    public class SubscriptionController
    {
        // Controller session and ID
        internal BBEngine.Session session;
        internal long ID;

        // Controller locker
        internal readonly Object controllerLocker = new Object();

        // Controller status (initialized in subscribing mode)
        private bool isSubscribing = true;
        public bool IsSubscribing
        {
            get { return this.isSubscribing; }
            internal set { this.isSubscribing = value; }
        }

        // Controller subscriptions
        internal Dictionary<string, SubscribedSecurity> subscriptions = new Dictionary<string, SubscribedSecurity>();
        internal Dictionary<string, SubscribedSecurity> Subscriptions{ get { return this.subscriptions; } }

        /// <summary>
        /// Gets a subscription by its correlation ID.
        /// </summary>
        /// <param name="correlationID">Correlation ID to look for.</param>
        /// <returns>Subscribed security matching the correlation ID.</returns>
        internal SubscribedSecurity GetSubscriptionByID(BBEngine.CorrelationID correlationID)
        {
            foreach (string item in this.subscriptions.Keys)
            {
                if (this.subscriptions[item].correlationID.Equals(correlationID))
                {
                    return this.subscriptions[item];
                }
            }
            return null;
        }

        /// <summary>
        /// Event handler of a subscription field update.
        /// </summary>
        public event EventHandler<SubscriptionEventArgs> SubscritionUpdate;
        internal void OnUpdate(SubscribedField field, SubscriptionEventArgs e)
        {
            if (SubscritionUpdate != null)
                SubscritionUpdate(field, e);
        }

        /// <summary>
        /// <c>SubscriptionController</c> constructor.
        /// </summary>
        /// <param name="session">Blpapi session element.</param>
        internal SubscriptionController(BBEngine.Session session, long ID)
        {
            this.session = session;
            this.ID = ID;
        }

        /// <summary>
        /// <c>SubscriptionController</c> finalizer.
        /// </summary>
        ~SubscriptionController()
        {
            this.session.subscriptionControllers.Remove(this.ID);
        }

        /// <summary>
        /// Builds a Blpapi subscription element.
        /// </summary>
        /// <param name="security">Ticker of the security to subscribe to.</param>
        /// <param name="fields">Set of fields to subscribe to.</param>
        /// <param name="parameters">Set of subscription parameters.</param>
        /// <returns>Blpapi subscription element.</returns>
        internal APISubscription GetAPISubscription(string security, Dictionary<string, SubscribedField> fields, Dictionary<string, dynamic> parameters)
        {
            // Security
            string APISecurity = security;

            // Fields
            List<string> APIFields = new List<string>();
            foreach (string item in fields.Keys)
                APIFields.Add(item);

            // Parameters
            List<string> APIParameters = new List<string>();
            foreach (string item in parameters.Keys)
                APIParameters.Add(item + "=" + parameters[item].ToString());

            // Correlation ID
            APICorrelationID APICorrelationID = new APICorrelationID(this.subscriptions[security].correlationID);

            // Return
            return new APISubscription(APISecurity, APIFields, APIParameters, APICorrelationID);
        }

        /// <summary>
        /// Adds a set of subscriptions to the subscription controller.
        /// </summary>
        /// <param name="subscriptions">Set of subscription elements.</param>
        public void AddSubscriptions(params BBEngine.Subscription[] subscriptions)
        {
            lock (controllerLocker)
            {
                foreach (BBEngine.Subscription item in subscriptions)
                {
                    if (item.fields.Count == 0)
                        System.Console.Error.WriteLine("BBEngine.Session[" + this.session + "][" + this.ID + ":" + BBEngine.ControlTypes.Subscription + "] << Can't subscribe/resubscribe to '" + item.security + "': fields missing");
                    else
                    {
                        if (this.subscriptions.ContainsKey(item.security))
                        {
                            this.subscriptions[item.security].UpdateFields(item.fields);
                            if (this.IsSubscribing)
                                this.session.thisAPISession.Resubscribe(new List<APISubscription> { GetAPISubscription(item.security, this.subscriptions[item.security].fields, this.subscriptions[item.security].parameters) });
                        }
                        else
                        {
                            this.subscriptions.Add(item.security, new SubscribedSecurity(this,item.security, item.fields, item.parameters));
                            if (this.IsSubscribing)
                                this.session.thisAPISession.Subscribe(new List<APISubscription> { GetAPISubscription(item.security, this.subscriptions[item.security].fields, this.subscriptions[item.security].parameters) });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes a set of subscriptions of the subscription controller.
        /// </summary>
        /// <param name="subscriptions">Set of securities to unsubscribe.</param>
        public void RemoveSubscriptions(params string[] securities)
        {
            lock (controllerLocker)
            {
                foreach (string item in securities)
                {
                    string index = BBEngine.Functions.Replace(item, " ").ToUpper();
                    if (this.subscriptions.ContainsKey(index))
                    {
                        if (this.IsSubscribing)
                            this.session.thisAPISession.Cancel(new APICorrelationID(this.subscriptions[index].correlationID));
                        this.subscriptions.Remove(index);
                    }
                }
            }
        }

        /// <summary>
        /// Removes all subscriptions of the subscription controller.
        /// </summary>
        public void ClearSubscriptions()
        {
            lock (controllerLocker)
            {
                foreach (string item in this.subscriptions.Keys)
                {
                    if (this.IsSubscribing)
                        this.session.thisAPISession.Cancel(new APICorrelationID(this.subscriptions[item].correlationID));
                    this.subscriptions.Remove(item);
                }
            }
        }

        /// <summary>
        /// Pauses subscribing (<c>IsSubscribing</c> is set to false).
        /// </summary>
        public void Pause()
        {
            lock (controllerLocker)
            {
                if (this.IsSubscribing)
                {
                    foreach (string item in this.subscriptions.Keys)
                        this.session.thisAPISession.Cancel(new APICorrelationID(this.subscriptions[item].correlationID));
                }

                this.IsSubscribing = false;
            }
        }

        /// <summary>
        /// Resumes subscribing (<c>IsSubscribing</c> is set to true).
        /// </summary>
        public void Resume()
        {
            lock (controllerLocker)
            {
                if (!this.IsSubscribing)
                {
                    foreach (string item in this.subscriptions.Keys)
                        this.session.thisAPISession.Subscribe(new List<APISubscription> { GetAPISubscription(item, this.subscriptions[item].fields, this.subscriptions[item].parameters) });
                }

                this.IsSubscribing = true;
            }
        }

        /// <summary>
        /// Fetchs bloomberg reponse data in the subscription controller.
        /// </summary>
        /// <param name="message">Blpapi message element.</param>
        internal void FetchData(APIMessage message, BBEngine.CorrelationID correlationID)
        {
            lock (controllerLocker)
            {
                // Get subscribed security
                SubscribedSecurity security = this.GetSubscriptionByID(correlationID);
                if (security != null)
                {
                    foreach (string item in security.fields.Keys)
                    {
                        try
                        {
                            if (message.HasElement(item))
                            {
                                // Get field element
                                APIElement fieldData = message.GetElement(item);

                                // Get field value
                                security.fields[item].Value = BBEngine.Functions.GetValueAsConverted<string>(fieldData);
                            }
                        }
                        catch (Exception e)
                        {
                            System.Console.WriteLine("BBEngine.Session[" + this.session + "][" + this.ID + ":" + correlationID + "].Security[" + security.ticker + "].Field[" + item + "] << " + e.Message);
                        }
                    }
                }

                return;
            }
        }
    }
}
