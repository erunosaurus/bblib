using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using APIElement = Bloomberglp.Blpapi.Element;
using APISubscription = Bloomberglp.Blpapi.Subscription;

namespace BBLib.BBEngine
{
    /// <summary>
    /// Enumerates control types
    /// </summary>
    internal enum ControlTypes
    {
        Reference = 0,
        Subscription = 1,
    }

    /// <summary>
    /// Designs a control correlation ID
    /// </summary>
    internal struct CorrelationID
    {
        public override string ToString() { return controlType.ToString() + ID.ToString(); }

        public readonly ControlTypes controlType;
        public readonly long ID;

        public CorrelationID(ControlTypes controlType, long ID)
        {
            this.controlType = controlType;
            this.ID = ID;
        }

    }

    /// <summary>
    /// Designs a requester response error element.
    /// </summary>
    internal class BBError
    {
        private readonly string type;
        public string Type { get { return type; } }

        private readonly string source;
        public string Source { get { return source; } }

        private readonly string code;
        public string Code { get { return code; } }

        private readonly string category;
        public string Category { get { return category; } }

        private readonly string message;
        public string Message { get { return message; } }

        private readonly string subcategory;
        public string Subcategory { get { return subcategory; } }

        public BBError(APIElement element)
        {
            // Type
            this.type = element.Name.ToString();

            // Source
            if (element.HasElement(BBEngine.Referential.SOURCE))
                this.source = element.GetElement(BBEngine.Referential.SOURCE).GetValueAsString();
            else
                this.source = "NA";

            // Code
            if (element.HasElement(BBEngine.Referential.CODE))
                this.code = element.GetElement(BBEngine.Referential.CODE).GetValueAsString();
            else if (element.HasElement(BBEngine.Referential.ERROR_CODE))
                this.code = element.GetElement(BBEngine.Referential.ERROR_CODE).GetValueAsString();
            else
                this.code = "NA";

            // Category
            if (element.HasElement(BBEngine.Referential.CATEGORY))
                this.category = element.GetElement(BBEngine.Referential.CATEGORY).GetValueAsString();
            else
                this.category = "NA";
            
            // Message
            if (element.HasElement(BBEngine.Referential.MESSAGE))
                this.message = element.GetElement(BBEngine.Referential.MESSAGE).GetValueAsString();
            else if (element.HasElement(BBEngine.Referential.DESCRIPTION))
                this.message = element.GetElement(BBEngine.Referential.DESCRIPTION).GetValueAsString();
            else
                this.message = "NA";

            // Subcategory
            if (element.HasElement(BBEngine.Referential.SUBCATEGORY))
                try
                {
                    this.subcategory = element.GetElement(BBEngine.Referential.SUBCATEGORY).GetValueAsString();
                }
                catch
                {
                    this.subcategory = "NA";
                }
            else
                this.subcategory = "NA";
        }

        public string Dump()
        {
            return
                this.type + " = {\n"
                    + "\tsource = " + this.source + "\n"
                    + "\tcode = " + this.code + "\n"
                    + "\tcategory = " + this.category + "\n"
                    + "\tmessage = " + this.message + "\n"
                    + "\tsubcategory = " + this.subcategory + "\n"
                + "}";
        }
    }

    /// <summary>
    /// Designs a subscription element.
    /// </summary>
    public class Subscription
    {
        // Subscription inputs
        internal readonly string security;
        internal Dictionary<string, System.Type> fields = new Dictionary<string, System.Type>();
        internal Dictionary<string, dynamic> parameters = new Dictionary<string, dynamic>();

        /// <summary>
        /// <c>BBSubscription</c> constructor.
        /// </summary>
        /// <param name="element">Security ticker (ticker may need to be prefixed, eg. /isin/... - see documentation).</param>
        public Subscription(string element)
        {
            this.security = Functions.Replace(element, " ").ToUpper();
        }

        /// <summary>
        /// Adds fields to the subscription element. 
        /// </summary>
        /// <param name="elements">Set of mnemonics and/or codes (field return types will be set to default, ie. as <c>string</c>).</param>
        public void AddFields(params string[] elements)
        {
            foreach (string item in elements)
            {
                string index = Functions.Replace(item, "_").ToUpper();
                if (!string.IsNullOrWhiteSpace(index)
                    && !BBEngine.Referential.ResponseCommonFieldIndex.ContainsKey(index)
                    && !this.fields.ContainsKey(index))
                {
                    this.fields.Add(index, typeof(string));
                }
            }
        }

        /// <summary>
        /// Adds a field to the subscription element.
        /// </summary>
        /// <param name="element">Field mnemonic or code.</param>
        /// <param name="value">Field return type.</param>
        public void AddField(string element, System.Type value)
        {
            string index = Functions.Replace(element, "_").ToUpper();
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
        /// Clears fields of the subscription element.
        /// </summary>
        public void ClearFields()
        {
            this.fields.Clear();
        }

        /// <summary>
        /// Sets a parameter to the subscription element (see documentation for available parameters).
        /// </summary>
        /// <param name="element">Parameter field.</param>
        /// <param name="value">Parameter value.</param>
        public void AddParameter(string element, dynamic value)
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
        /// Clears parameters of the subscription element.
        /// </summary>
        public void ClearParameters()
        {
            this.parameters.Clear();
        }

        /// <summary>
        /// Clears all inputs of the subscription element.
        /// </summary>
        public void Clear()
        {
            this.fields.Clear();
            this.parameters.Clear();
        }
    }
}
