using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using APICorrelationID = Bloomberglp.Blpapi.CorrelationID;
using APIElement = Bloomberglp.Blpapi.Element;
using APIMessage = Bloomberglp.Blpapi.Message;
using APIRequest = Bloomberglp.Blpapi.Request;

namespace BBLib.BBControl
{
    /// <summary>
    /// Designs a reference requester.
    /// </summary>
    public abstract class ReferenceRequester
    {
        // Requester session and request type
        private BBEngine.Session session;
        private string requestType;

        // Requester inputs
        private List<string> securities = new List<string>();
        private List<string> eventtypes = new List<string>();
        private Dictionary<string, System.Type> fields = new Dictionary<string, System.Type>();
        private Dictionary<string, string> overrides = new Dictionary<string, string>();
        private Dictionary<string, dynamic> parameters = new Dictionary<string, dynamic>();

        // Requester response
        private BBEngine.CorrelationID correlationID;
        private int numResponses = 0;
        private DataSet response = null;
        private string responseError = null;
        internal AutoResetEvent responseStatus;

        /// <summary>
        /// <c>ReferenceRequester</c> constructor.
        /// </summary>
        /// <param name="session">Blpapi session element.</param>
        /// <param name="correlationID">Requester correlation ID.</param>
        private ReferenceRequester(BBEngine.Session session, BBEngine.CorrelationID correlationID)
        {
            this.session = session;
            this.correlationID = correlationID;
            this.responseStatus = new AutoResetEvent(false);
        }

        /// <summary>
        /// <c>ReferenceRequester</c> finalizer.
        /// </summary>
        ~ReferenceRequester()
        {
            this.session.references.Remove(this.correlationID);
        }

        /// <summary>
        /// Fetchs bloomberg reponse data in the requester <c>DataSet reponse</c> element.
        /// </summary>
        /// <param name="message">Blpapi message element.</param>
        internal abstract void FetchData(APIMessage message);

        /// <summary>
        /// Add security tickers to the requester.
        /// </summary>
        /// <param name="elements">Set of security tickers (tickers may need to be prefixed, eg. /isin/... - see documentation).</param>
        public virtual void AddSecurities(params string[] elements)
        {
            foreach (string item in elements)
            {
                if (!string.IsNullOrWhiteSpace(item) && !this.securities.Contains(item))
                    this.securities.Add(BBEngine.Functions.Replace(item, " ").ToUpper());
            }
        }

        /// <summary>
        /// Clears security tickers of the requester.
        /// </summary>
        public virtual void ClearSecurities()
        {
            this.securities.Clear();
        }

        /// <summary>
        /// Add event types to the requester (<c>IntradayBar</c> can only take one event type / not applicable for <c>ReferenceData</c> and <c>HistoricalData</c>).
        /// </summary>
        /// <param name="element">Set of event types.</param>
        public virtual void AddEventTypes(params string[] element)
        {
            foreach (string item in element)
            {
                if (!string.IsNullOrWhiteSpace(item) && !this.eventtypes.Contains(item))
                    this.eventtypes.Add(BBEngine.Functions.Replace(item, "_").ToUpper());
            }
        }

        /// <summary>
        /// Clears event types of the requester (not applicable for <c>ReferenceData</c> and <c>HistoricalData</c>).
        /// </summary>
        public virtual void ClearEventTypes()
        {
            this.eventtypes.Clear();
        }

        /// <summary>
        /// Adds fields to the requester (not applicable for <c>IntradayTick</c> and <c>IntradayBar</c>). 
        /// </summary>
        /// <param name="elements">Set of mnemonics and/or codes (field return types will be set to default, ie. as <c>string</c>).</param>
        public virtual void AddFields(params string[] elements)
        {
            foreach (string item in elements)
            {
                string index = BBEngine.Functions.Replace(item, "_").ToUpper();
                if (!string.IsNullOrWhiteSpace(index)
                    && !BBEngine.Referential.ResponseCommonFieldIndex.ContainsKey(index)
                    && !this.fields.ContainsKey(index))
                {
                    this.fields.Add(index, typeof(string));
                }
            }
        }

        /// <summary>
        /// Adds a field to the requester (not applicable for <c>IntradayTick</c> and <c>IntradayBar</c>).
        /// </summary>
        /// <param name="element">Field mnemonic or code.</param>
        /// <param name="value">Field return type.</param>
        public virtual void AddField(string element, System.Type value)
        {
            string index = BBEngine.Functions.Replace(element, "_").ToUpper();
            if (!string.IsNullOrWhiteSpace(index)
                && !BBEngine.Referential.ResponseCommonFieldIndex.ContainsKey(index))
            {
                if (this.fields.ContainsKey(index))
                    this.fields[index] = value;
                else
                    this.fields.Add(index, value);
            }
        }

        /// <summary>
        /// Clears fields of the requester (not applicable for <c>IntradayTick</c> and <c>IntradayBar</c>).
        /// </summary>
        public virtual void ClearFields()
        {
            this.fields.Clear();
        }

        /// <summary>
        /// Sets a field override to the requester (not applicable for <c>IntradayTick</c> and <c>IntradayBar</c>).
        /// </summary>
        /// <param name="element">Field mnemonic or code.</param>
        /// <param name="value">Override value.</param>
        public virtual void AddOverride(string element, string value)
        {
            string index = BBEngine.Functions.Replace(element, "_").ToUpper();
            if (!string.IsNullOrWhiteSpace(index) && !string.IsNullOrWhiteSpace(value))
            {
                if (this.overrides.ContainsKey(index))
                    this.overrides[index] = value;
                else
                    this.overrides.Add(index, value);
            }
        }

        /// <summary>
        /// Clears overrides of the requester (not applicable for <c>IntradayTick</c> and <c>IntradayBar</c>).
        /// </summary>
        public virtual void ClearOverrides()
        {
            this.overrides.Clear();
        }

        /// <summary>
        /// Sets a parameter to the requester (see documentation for available parameters).
        /// </summary>
        /// <param name="element">Parameter field.</param>
        /// <param name="value">Parameter value.</param>
        public virtual void AddParameter(string element, dynamic value)
        {
            if (!string.IsNullOrWhiteSpace(element))
            {
                if (this.parameters.ContainsKey(element))
                    this.parameters[element] = value;
                else
                    this.parameters.Add(element, value);
            }
        }

        /// <summary>
        /// Clears parameters of the requester.
        /// </summary>
        public virtual void ClearParameters()
        {
            this.parameters.Clear();
        }

        /// <summary>
        /// Clears all inputs of the requester.
        /// </summary>
        public virtual void Clear()
        {
            this.securities.Clear();
            this.fields.Clear();
            this.overrides.Clear();
            this.parameters.Clear();
        }

        /// <summary>
        /// Sends a request feed with the requester inputs to bloomberg.
        /// </summary>
        /// <param name="response">Set of data where the bloomberg response is fetched.</param>
        /// <returns>The <c>Send</c> procedure state (false indicates a response error).</returns>
        public bool SendRequest(out DataSet response)
        {
            string error;
            return SendRequest(out response, out error);
        }

        /// <summary>
        /// Sends a request feed with the requester inputs to bloomberg.
        /// </summary>
        /// <param name="response">Set of data where the bloomberg response is fetched.</param>
        /// <param name="error">Description of the <c>Send</c> procedure error.</param>
        /// <returns>The <c>Send</c> procedure state (false indicates a response error).</returns>
        public bool SendRequest(out DataSet response, out string error)
        {
            this.responseError = null;

            // Check inputs
            if (((this.requestType == BBEngine.Referential.HSTDATA_REQ || this.requestType == BBEngine.Referential.REFDATA_REQ) && (securities.Count == 0 || fields.Count == 0))
                || (this.requestType == BBEngine.Referential.IDBDATA_REQ && (securities.Count == 0 || eventtypes.Count != 1))
                || (this.requestType == BBEngine.Referential.IDTDATA_REQ && (securities.Count == 0 || eventtypes.Count == 0)))
            {
                this.responseError = "Invalid inputs";
                System.Console.Error.WriteLine("BBEngine.Session[" + this.session + "][" + this.correlationID + "]." + this.requestType  + " << " + this.responseError);
                response = this.response;
                error = this.responseError;
                return false;
            }

            // Design the requester response data set
            this.response = new DataSet(correlationID.ToString());
            foreach (string security in this.securities)
            {
                DataTable table = new DataTable(security);
                table.Columns.Add(BBEngine.Referential.DS_ID, typeof(string));
                table.Columns.Add(BBEngine.Referential.DS_RELATIVE_DATE, typeof(DateTime));
                table.Columns.Add(BBEngine.Referential.DS_DATE, typeof(DateTime));
                foreach (string item in this.fields.Keys)
                {
                    table.Columns.Add(item, fields[item]);
                }
                this.response.Tables.Add(table);
            }

            // Request
            int numRequests = 1;
            switch (this.requestType)
            {
                case BBEngine.Referential.IDBDATA_REQ:
                case BBEngine.Referential.IDTDATA_REQ:
                    {
                        numRequests = this.securities.Count;
                        break;
                    }
                default:
                    break;
            }

            this.numResponses = 0;
            try
            {
                for (int i = 0; i < numRequests; ++i)
                {
                    // Create and feed request
                    APIRequest request = this.session.services[BBEngine.Referential.REFDATA_SVC].Reference.CreateRequest(this.requestType);
                    FeedRequest(request, i);

                    // Send request
                    this.session.thisAPISession.SendRequest(request, new APICorrelationID(this.correlationID));

                    // Wait response to be filled
                    this.responseStatus.WaitOne();

                    ++numResponses;
                }

                // Return and handle response error
                if (this.responseError != null)
                {
                    response = null;
                    error = this.responseError;
                    return false;
                }
                else
                {
                    response = this.response;
                    error = this.responseError;
                    return true;
                }
            }
            catch (Exception e)
            {
                System.Console.Error.WriteLine("BBEngine.Session[" + this.session + "][" + this.correlationID + "]." + this.requestType + " << " + e.Message);
                response = this.response;
                error = this.responseError;
                return false;
            }
        }

        /// <summary>
        /// Feeds Blpapi request element with requester inputs.
        /// </summary>
        /// <param name="request">Blpapi request element to feed.</param>
        /// <param name="securities">Requester securities.</param>
        /// <param name="fields">Requester fields.</param>
        /// <param name="overrides">Requester overrides.</param>
        /// <param name="parameters">Requester parameters.</param>
        private void FeedRequest(APIRequest request, int numRequest)
        {
            // Implement different securities and fields feed process by request type
            switch (this.requestType)
            {
                case BBEngine.Referential.HSTDATA_REQ:
                case BBEngine.Referential.REFDATA_REQ:
                    {
                        // Feed securities
                        APIElement APISecurities = request.GetElement(BBEngine.Referential.SECURITIES);
                        foreach (string item in this.securities)
                        {
                            APISecurities.AppendValue(item);
                        }

                        // Feed fields
                        APIElement APIFields = request.GetElement(BBEngine.Referential.FIELDS);
                        foreach (string item in this.fields.Keys)
                        {
                            APIFields.AppendValue(item);
                        }

                        // Feed overrides
                        if (this.overrides.Count > 0)
                        {
                            APIElement APIOverrides = request[BBEngine.Referential.OVERRIDES];
                            foreach (string item in this.overrides.Keys)
                            {
                                APIElement APIOverride = APIOverrides.AppendElement();
                                APIOverride.SetElement(BBEngine.Referential.FIELD_ID, item);
                                APIOverride.SetElement(BBEngine.Referential.VALUE, overrides[item]);
                            }
                        }
                        break;
                    }
                case BBEngine.Referential.IDBDATA_REQ:
                    {
                        // Feed security
                        request.Set(BBEngine.Referential.SECURITY, this.securities[numRequest]);

                        // Feed event type
                        try
                        {
                            request.Set(BBEngine.Referential.EVENT_TYPE, this.eventtypes[0]);
                        }
                        catch
                        {
                            this.responseError = "Invalid input event type: " + this.eventtypes[0];
                            throw new ArgumentException(this.responseError);
                        }
                        break;
                    }
                case BBEngine.Referential.IDTDATA_REQ:
                    {
                        // Feed security
                        request.Set(BBEngine.Referential.SECURITY, this.securities[numRequest]);

                        // Feed event types
                        APIElement APIEventTypes = request.GetElement(BBEngine.Referential.EVENT_TYPES);
                        foreach (string item in this.eventtypes)
                        {
                            try
                            {
                                APIEventTypes.AppendValue(item);
                            }
                            catch
                            {
                                this.responseError = "Invalid input event type: " + item;
                                throw new ArgumentException(this.responseError);
                            }
                        }
                        break;
                    }
            }

            // Feed parameters
            if (this.parameters.Count > 0)
            {
                foreach (string item in this.parameters.Keys)
                {
                    try
                    {
                        request.Set(item, parameters[item]);
                    }
                    catch
                    {
                        this.responseError = "Invalid input parameter: " + item;
                        throw new ArgumentException(this.responseError);
                    }
                }
            }
        }

        /// <summary>
        /// Designs an <c>HistoricalData</c> requester.
        /// </summary>
        internal class HistoricalData : ReferenceRequester
        {
            /// <summary>
            /// <c>HistoricalData</c> constructor.
            /// </summary>
            /// <param name="session">Blpapi session element.</param>
            /// <param name="correlationID">Requester correlation ID.</param>
            public HistoricalData(BBEngine.Session session, BBEngine.CorrelationID correlationID)
                : base(session, correlationID)
            {
                this.requestType = BBEngine.Referential.HSTDATA_REQ;
            }

            /// <summary>
            /// Add event types to the requester (<c>IntradayBar</c> can only take one event type / not applicable for <c>ReferenceData</c> and <c>HistoricalData</c>).
            /// </summary>
            /// <param name="eventtypes">Set of event types.</param>
            public override void AddEventTypes(params string[] eventtypes) { }

            /// <summary>
            /// Clears event types of the requester (not applicable for <c>ReferenceData</c> and <c>HistoricalData</c>).
            /// </summary>
            public override void ClearEventTypes() { }

            /// <summary>
            /// Fetchs bloomberg reponse data in the requester <c>DataSet reponse</c> element.
            /// </summary>
            /// <param name="message">Blpapi message element.</param>
            internal override void FetchData(APIMessage message)
            {
                // Handle Blpapi response error element
                if (message.HasElement(BBEngine.Referential.RESPONSE_ERROR))
                {
                    BBEngine.BBError bbError = new BBEngine.BBError(message.GetElement(BBEngine.Referential.RESPONSE_ERROR));
                    Console.Error.WriteLine("BBEngine.Session[" + this.session + "][" + this.correlationID + "]." + this.requestType + " << " + bbError.Dump());
                    this.responseError = bbError.Message;
                    throw new Exception();
                }

                // Get security
                APIElement securityData = message.GetElement(BBEngine.Referential.SECURITY_DATA);
                int sequenceNumber = securityData.GetElementAsInt32(BBEngine.Referential.SEQUENCE_NUMBER);
                string securityName = securityData.GetElementAsString(BBEngine.Referential.SECURITY);

                // Instanciate a new data set row
                DataRow modelRow = this.response.Tables[sequenceNumber].NewRow();
                modelRow.SetField(BBEngine.Referential.DS_ID, securityName);

                // Handle Blpapi security error element
                if (securityData.HasElement(BBEngine.Referential.SECURITY_ERROR))
                {
                    BBEngine.BBError bbError = new BBEngine.BBError(securityData.GetElement(BBEngine.Referential.SECURITY_ERROR));
                    System.Console.Error.WriteLine("BBEngine.Session[" + this.session + "][" + this.correlationID + "]." + this.requestType + ".Security(" + securityName + ") << " + bbError.Dump());
                    modelRow.RowError = bbError.Message;
                    this.response.Tables[sequenceNumber].Rows.Add(modelRow);
                    return;
                }

                // Retrieve list of field exceptions
                APIElement fieldExceptions = securityData.GetElement(BBEngine.Referential.FIELD_EXCEPTIONS);
                int numFieldExceptions = fieldExceptions.NumValues;
                for (int i = 0; i < numFieldExceptions; ++i)
                {
                    APIElement fieldException = fieldExceptions.GetValueAsElement(i);
                    string fieldId = fieldException.GetElementAsString(BBEngine.Referential.FIELD_ID);
                    BBEngine.BBError bbError = new BBEngine.BBError(fieldException.GetElement(BBEngine.Referential.ERROR_INFO));
                    System.Console.Error.WriteLine("BBEngine.Session[" + this.session + "][" + this.correlationID + "]." + this.requestType + ".Security[" + securityName + "].Field[" + fieldId + "] << " + bbError.Dump());
                    modelRow.SetColumnError(fieldId, bbError.Message);
                }

                // Retrieve list of datetimes
                APIElement datetimeDataArray = securityData.GetElement(BBEngine.Referential.FIELD_DATA);
                int numDatetimes = datetimeDataArray.NumValues;
                for (int i = 0; i < numDatetimes; ++i)
                {
                    // Copy model row
                    DataRow newRow = BBEngine.Functions.CopyDataRow(this.response.Tables[sequenceNumber], modelRow);
                    
                    // Retrieve list of fields
                    APIElement fieldDataArray = datetimeDataArray.GetValueAsElement(i);
                    int numFields = fieldDataArray.NumElements;
                    for (int j = 0; j < numFields; ++j)
                    {
                        // Get field
                        APIElement fieldData = fieldDataArray.GetElement(j);
                        string fieldName = fieldData.Name.ToString();
                        System.Type fieldType = null;
                        if (fieldName == BBEngine.Referential.RELATIVE_DATE.ToString() || fieldName == BBEngine.Referential.DATE.ToString())
                        {
                            newRow.SetField(BBEngine.Referential.ResponseCommonFieldIndex[fieldName], BBEngine.Functions.GetValueAsConverted<DateTime>(fieldData));
                            continue;
                        }
                        else
                        {
                            foreach (string item in fields.Keys)
                            {
                                if (item == fieldName)
                                {
                                    fieldType = fields[item];
                                    break;
                                }
                            }
                        }
                        
                        // Retrieve and convert field value
                        MethodInfo method = typeof(BBEngine.Functions).GetMethod("GetValueAsConverted");
                        method = method.MakeGenericMethod(fieldType);
                        try
                        {
                            newRow.SetField(fieldName, method.Invoke(null, new object[] { fieldData }));
                        }
                        catch (Exception e)
                        {
                            this.responseError = e.Message;
                            System.Console.Error.WriteLine("BBEngine.Session[" + this.session + "][" + this.correlationID + "]." + this.requestType + ".Security[" + securityName + "].Field[" + fieldName + "] << " + this.responseError);
                            newRow.SetColumnError(fieldName, this.responseError);
                        }
                    }

                    // Add data set row to the requester response element
                    this.response.Tables[sequenceNumber].Rows.Add(newRow);
                }
            }
        }

        /// <summary>
        /// Designs an <c>IntradayBar</c> requester.
        /// </summary>
        internal class IntradayBar : ReferenceRequester
        {
            /// <summary>
            /// <c>IntradayBar</c> constructor.
            /// </summary>
            /// <param name="session">Blpapi session element.</param>
            /// <param name="correlationID">Requester correlation ID.</param>
            public IntradayBar(BBEngine.Session session, BBEngine.CorrelationID correlationID)
                : base(session, correlationID)
            {
                this.requestType = BBEngine.Referential.IDBDATA_REQ;

                // Set fields
                this.fields.Add(BBEngine.Referential.DS_OPEN, typeof(double));
                this.fields.Add(BBEngine.Referential.DS_HIGH, typeof(double));
                this.fields.Add(BBEngine.Referential.DS_LOW, typeof(double));
                this.fields.Add(BBEngine.Referential.DS_CLOSE, typeof(double));
                this.fields.Add(BBEngine.Referential.DS_VOLUME, typeof(long));
                this.fields.Add(BBEngine.Referential.DS_NUM_EVENTS, typeof(long));
                this.fields.Add(BBEngine.Referential.DS_VALUE, typeof(double));
            }

            /// <summary>
            /// Adds fields to the requester (not applicable for <c>IntradayTick</c> and <c>IntradayBar</c>). 
            /// </summary>
            /// <param name="fields">Set of mnemonics and/or codes (field return types will be set to default, ie. as <c>string</c>).</param>
            public override void AddFields(params string[] fields) { }

            /// <summary>
            /// Adds a field to the requester (not applicable for <c>IntradayTick</c> and <c>IntradayBar</c>).
            /// </summary>
            /// <param name="element">Field mnemonic or code.</param>
            /// <param name="value">Field return type.</param>
            public override void AddField(string element, System.Type value) { }

            /// <summary>
            /// Clears fields of the requester (not applicable for <c>IntradayTick</c> and <c>IntradayBar</c>).
            /// </summary>
            public override void ClearFields() { }

            /// <summary>
            /// Sets a field override to the requester (not applicable for <c>IntradayTick</c> and <c>IntradayBar</c>).
            /// </summary>
            /// <param name="element">Field mnemonic or code.</param>
            /// <param name="value">Override value.</param>
            public override void AddOverride(string element, string value) { }

            /// <summary>
            /// Clears overrides of the requester (not applicable for <c>IntradayTick</c> and <c>IntradayBar</c>).
            /// </summary>
            public override void ClearOverrides() { }

            /// <summary>
            /// Fetchs bloomberg reponse data in the requester <c>DataSet reponse</c> element.
            /// </summary>
            /// <param name="message">Blpapi message element.</param>
            internal override void FetchData(APIMessage message)
            {
                // Security ID
                int sequenceNumber = numResponses;
                string securityName = securities[sequenceNumber];

                // Instanciate a new data set row
                DataRow modelRow = this.response.Tables[sequenceNumber].NewRow();
                modelRow.SetField(BBEngine.Referential.DS_ID, securityName);

                // Handle Blpapi response error element
                if (message.HasElement(BBEngine.Referential.RESPONSE_ERROR))
                {
                    BBEngine.BBError bbError = new BBEngine.BBError(message.GetElement(BBEngine.Referential.RESPONSE_ERROR));
                    System.Console.Error.WriteLine("BBEngine.Session[" + this.session + "][" + this.correlationID + "]." + this.requestType + ".Security[" + securityName + "] << " + bbError.Dump());
                    if (bbError.Code == "15")
                    {
                        modelRow.RowError = bbError.Message;
                        this.response.Tables[sequenceNumber].Rows.Add(modelRow);
                    }
                    else
                        this.responseError = (this.responseError == null) ? bbError.Message : this.responseError + " + " + bbError.Message;
                    throw new Exception();
                }

                // Get security data
                APIElement securityData = message.GetElement(BBEngine.Referential.BAR_DATA);

                // Retrieve list of bar data
                APIElement barDataArray = securityData.GetElement(BBEngine.Referential.BAR_TICK_DATA);
                int numBars = barDataArray.NumValues;
                for (int i = 0; i < numBars; ++i)
                {
                    // Copy model row
                    DataRow newRow = BBEngine.Functions.CopyDataRow(this.response.Tables[sequenceNumber], modelRow);

                    // Retrieve list of fields
                    APIElement fieldDataArray = barDataArray.GetValueAsElement(i);
                    int numFields = fieldDataArray.NumElements;
                    for (int j = 0; j < numFields; ++j)
                    {
                        // Get field
                        APIElement fieldData = fieldDataArray.GetElement(j);
                        string fieldName = fieldData.Name.ToString();
                        System.Type fieldType = null;
                        if (fieldName == BBEngine.Referential.TIME.ToString())
                        {
                            newRow.SetField(BBEngine.Referential.DS_DATE, BBEngine.Functions.GetValueAsConverted<DateTime>(fieldData));
                            continue;
                        }
                        else
                        {
                            foreach (string item in this.fields.Keys)
                            {
                                string t = BBEngine.Referential.ResponseCommonFieldIndex[fieldName];
                                if (item == BBEngine.Referential.ResponseCommonFieldIndex[fieldName])
                                {
                                    fieldType = fields[item];
                                    break;
                                }
                            }
                        }

                        // Retrieve and convert field value
                        MethodInfo method = typeof(BBEngine.Functions).GetMethod("GetValueAsConverted");
                        method = method.MakeGenericMethod(fieldType);
                        try
                        {
                            newRow.SetField(BBEngine.Referential.ResponseCommonFieldIndex[fieldName], method.Invoke(null, new object[] { fieldData }));
                        }
                        catch (Exception e)
                        {
                            this.responseError = e.Message;
                            System.Console.Error.WriteLine("BBEngine.Session[" + this.session + "][" + this.correlationID + "]." + this.requestType + ".Security[" + securityName + "].Field[" + fieldName + "] << " + this.responseError);
                            newRow.SetColumnError(fieldName, this.responseError);
                        }
                    }

                    // Add data set row to the requester response element
                    this.response.Tables[sequenceNumber].Rows.Add(newRow);
                }
            }
        }

        /// <summary>
        /// Designs an <c>IntradayTick</c> requester.
        /// </summary>
        internal class IntradayTick : ReferenceRequester
        {
            /// <summary>
            /// <c>IntradayTick</c> constructor.
            /// </summary>
            /// <param name="session">Blpapi session element.</param>
            /// <param name="correlationID">Requester correlation ID.</param>
            public IntradayTick(BBEngine.Session session, BBEngine.CorrelationID correlationID)
                : base(session, correlationID)
            {
                this.requestType = BBEngine.Referential.IDTDATA_REQ;

                // Set fields
                this.fields.Add(BBEngine.Referential.DS_TYPE, typeof(string));
                this.fields.Add(BBEngine.Referential.DS_VALUE, typeof(double));
                this.fields.Add(BBEngine.Referential.DS_SIZE, typeof(int));
                this.fields.Add(BBEngine.Referential.DS_CONDITION_CODE, typeof(string));
                this.fields.Add(BBEngine.Referential.DS_EXCHANGE_CODE, typeof(string));
                this.fields.Add(BBEngine.Referential.DS_MIC_CODE, typeof(string));
                this.fields.Add(BBEngine.Referential.DS_BROKER_BUY_CODE, typeof(string));
                this.fields.Add(BBEngine.Referential.DS_BROKER_SELL_CODE, typeof(string));
                this.fields.Add(BBEngine.Referential.DS_RPS_CODE, typeof(string));
            }

            /// <summary>
            /// Adds fields to the requester (not applicable for <c>IntradayTick</c> and <c>IntradayBar</c>). 
            /// </summary>
            /// <param name="fields">Set of mnemonics and/or codes (field return types will be set to default, ie. as <c>string</c>).</param>
            public override void AddFields(params string[] fields) { }

            /// <summary>
            /// Adds a field to the requester (not applicable for <c>IntradayTick</c> and <c>IntradayBar</c>).
            /// </summary>
            /// <param name="element">Field mnemonic or code.</param>
            /// <param name="value">Field return type.</param>
            public override void AddField(string element, System.Type value) { }

            /// <summary>
            /// Clears fields of the requester (not applicable for <c>IntradayTick</c> and <c>IntradayBar</c>).
            /// </summary>
            public override void ClearFields() { }
            
            /// <summary>
            /// Sets a field override to the requester (not applicable for <c>IntradayTick</c> and <c>IntradayBar</c>).
            /// </summary>
            /// <param name="element">Field mnemonic or code.</param>
            /// <param name="value">Override value.</param>
            public override void AddOverride(string element, string value) { }

            /// <summary>
            /// Clears overrides of the requester (not applicable for <c>IntradayTick</c> and <c>IntradayBar</c>).
            /// </summary>
            public override void ClearOverrides() { }

            /// <summary>
            /// Fetchs bloomberg reponse data in the requester <c>DataSet reponse</c> element.
            /// </summary>
            /// <param name="message">Blpapi message element.</param>
            internal override void FetchData(APIMessage message)
            {
                // Security ID
                int sequenceNumber = numResponses;
                string securityName = securities[sequenceNumber];

                // Instanciate a new data set row
                DataRow modelRow = this.response.Tables[sequenceNumber].NewRow();
                modelRow.SetField(BBEngine.Referential.DS_ID, securityName);

                // Handle Blpapi response error element
                if (message.HasElement(BBEngine.Referential.RESPONSE_ERROR))
                {
                    BBEngine.BBError bbError = new BBEngine.BBError(message.GetElement(BBEngine.Referential.RESPONSE_ERROR));
                    System.Console.Error.WriteLine("BBEngine.Session[" + this.session + "][" + this.correlationID + "]." + this.requestType + ".Security[" + securityName + "] << " + bbError.Dump());
                    if (bbError.Code == "15")
                    {
                        modelRow.RowError = bbError.Message;
                        this.response.Tables[sequenceNumber].Rows.Add(modelRow);
                    }
                    else
                        this.responseError = (this.responseError == null) ? bbError.Message : this.responseError + " + " + bbError.Message;
                    throw new Exception();
                }

                // Get security data
                APIElement securityData = message.GetElement(BBEngine.Referential.TICK_DATA);

                // Retrieve list of tick data
                APIElement tickDataArray = securityData.GetElement(BBEngine.Referential.TICK_DATA);
                int numTicks = tickDataArray.NumValues;
                for (int i = 0; i < numTicks; ++i)
                {
                    // Copy model row
                    DataRow newRow = BBEngine.Functions.CopyDataRow(this.response.Tables[sequenceNumber], modelRow);

                    // Retrieve list of fields
                    APIElement fieldDataArray = tickDataArray.GetValueAsElement(i);
                    int numFields = fieldDataArray.NumElements;
                    for (int j = 0; j < numFields; ++j)
                    {
                        // Get field
                        APIElement fieldData = fieldDataArray.GetElement(j);
                        string fieldName = fieldData.Name.ToString();
                        System.Type fieldType = null;
                        if (fieldName == BBEngine.Referential.TIME.ToString())
                        {
                            newRow.SetField(BBEngine.Referential.DS_DATE, BBEngine.Functions.GetValueAsConverted<DateTime>(fieldData));
                            continue;
                        }
                        else
                        {
                            foreach (string item in this.fields.Keys)
                            {
                                if (item == BBEngine.Referential.ResponseCommonFieldIndex[fieldName])
                                {
                                    fieldType = fields[item];
                                    break;
                                }
                            }
                        }

                        // Retrieve and convert field value
                        MethodInfo method = typeof(BBEngine.Functions).GetMethod("GetValueAsConverted");
                        method = method.MakeGenericMethod(fieldType);
                        try
                        {
                            newRow.SetField(BBEngine.Referential.ResponseCommonFieldIndex[fieldName], method.Invoke(null, new object[] { fieldData }));
                        }
                        catch (Exception e)
                        {
                            this.responseError = e.Message;
                            System.Console.Error.WriteLine("BBEngine.Session[" + this.session + "][" + this.correlationID + "]." + this.requestType + ".Security[" + securityName + "].Field[" + fieldName + "] << " + this.responseError);
                            newRow.SetColumnError(fieldName, this.responseError);
                        }
                    }

                    // Add data set row to the requester response element
                    this.response.Tables[sequenceNumber].Rows.Add(newRow);
                }
            }
        }

        /// <summary>
        /// Designs a <c>ReferenceData</c> requester.
        /// </summary>
        internal class ReferenceData : ReferenceRequester
        {
            /// <summary>
            /// <c>ReferenceData</c> constructor.
            /// </summary>
            /// <param name="session">Blpapi session element.</param>
            /// <param name="correlationID">Requester correlation ID.</param>
            public ReferenceData(BBEngine.Session session, BBEngine.CorrelationID correlationID)
                : base(session, correlationID)
            {
                this.requestType = BBEngine.Referential.REFDATA_REQ;
            }

            /// <summary>
            /// Add event types to the requester (<c>IntradayBar</c> can only take one event type / not applicable for <c>ReferenceData</c> and <c>HistoricalData</c>).
            /// </summary>
            /// <param name="eventtypes">Set of event types.</param>
            public override void AddEventTypes(params string[] eventtypes) { }

            /// <summary>
            /// Clears event types of the requester (not applicable for <c>ReferenceData</c> and <c>HistoricalData</c>).
            /// </summary>
            public override void ClearEventTypes() { }

            /// <summary>
            /// Fetchs bloomberg reponse data in the requester <c>DataSet reponse</c> element.
            /// </summary>
            /// <param name="message">Blpapi message element.</param>
            internal override void FetchData(APIMessage message)
            {
                // Handle Blpapi response error element
                if (message.HasElement(BBEngine.Referential.RESPONSE_ERROR))
                {
                    BBEngine.BBError bbError = new BBEngine.BBError(message.GetElement(BBEngine.Referential.RESPONSE_ERROR));
                    Console.Error.WriteLine("BBEngine.Session[" + this.session + "][" + this.correlationID + "]." + this.requestType + " << " + bbError.Dump());
                    this.responseError = bbError.Message;
                    throw new Exception();
                }

                // Retrieve list of securities
                APIElement securityDataArray = message.GetElement(BBEngine.Referential.SECURITY_DATA);
                int numSecurities = securityDataArray.NumValues;
                for (int i = 0; i < numSecurities; ++i)
                {
                    // Get security
                    APIElement securityData = securityDataArray.GetValueAsElement(i);
                    int sequenceNumber = securityData.GetElementAsInt32(BBEngine.Referential.SEQUENCE_NUMBER);
                    string securityName = securityData.GetElementAsString(BBEngine.Referential.SECURITY);

                    // Instanciate a new data set row
                    DataRow modelRow = this.response.Tables[sequenceNumber].NewRow();
                    modelRow.SetField(BBEngine.Referential.DS_ID, securityName);

                    // Handle Blpapi security error element
                    if (securityData.HasElement(BBEngine.Referential.SECURITY_ERROR))
                    {
                        BBEngine.BBError bbError = new BBEngine.BBError(securityData.GetElement(BBEngine.Referential.SECURITY_ERROR));
                        System.Console.Error.WriteLine("BBEngine.Session[" + this.session + "][" + this.correlationID + "]." + this.requestType + ".Security[" + securityName + "] << " + bbError.Dump());
                        modelRow.RowError = bbError.Message;
                        this.response.Tables[sequenceNumber].Rows.Add(modelRow);
                        continue;
                    }

                    // Retrieve list of field exceptions
                    APIElement fieldExceptions = securityData.GetElement(BBEngine.Referential.FIELD_EXCEPTIONS);
                    int numFieldExceptions = fieldExceptions.NumValues;
                    for (int j = 0; j < numFieldExceptions; ++j)
                    {
                        APIElement fieldException = fieldExceptions.GetValueAsElement(j);
                        string fieldId = fieldException.GetElementAsString(BBEngine.Referential.FIELD_ID);
                        BBEngine.BBError bbError = new BBEngine.BBError(fieldException.GetElement(BBEngine.Referential.ERROR_INFO));
                        System.Console.Error.WriteLine("BBEngine.Session[" + this.session + "][" + this.correlationID + "]." + this.requestType + ".Security[" + securityName + "].Field[" + fieldId + "] << " + bbError.Dump());
                        modelRow.SetColumnError(fieldId, bbError.Message);
                    }

                    // Retrieve list of fields
                    APIElement fieldDataArray = securityData.GetElement(BBEngine.Referential.FIELD_DATA);
                    int numFields = fieldDataArray.NumElements;
                    for (int j = 0; j < numFields; ++j)
                    {
                        // Get field
                        APIElement fieldData = fieldDataArray.GetElement(j);
                        string fieldName = fieldData.Name.ToString();
                        System.Type fieldType = null;
                        foreach (string item in this.fields.Keys)
                        {
                            if (item == fieldName)
                            {
                                fieldType = fields[item];
                                break;
                            }
                        }

                        // Retrieve and convert field value
                        MethodInfo method = typeof(BBEngine.Functions).GetMethod("GetValueAsConverted");
                        method = method.MakeGenericMethod(fieldType);
                        try
                        {
                            modelRow.SetField(fieldName, method.Invoke(null, new object[] { fieldData }));
                        }
                        catch (Exception e)
                        {
                            this.responseError = e.ToString();
                            System.Console.Error.WriteLine("BBEngine.Session[" + this.session + "][" + this.correlationID + "]." + this.requestType + ".Security[" + securityName + "].Field[" + fieldName + "] << " + this.responseError);
                            modelRow.SetColumnError(fieldName, this.responseError);
                        }
                    }

                    // Add data set row to the requester response element
                    this.response.Tables[sequenceNumber].Rows.Add(modelRow);
                }
            }
        }
    }
}