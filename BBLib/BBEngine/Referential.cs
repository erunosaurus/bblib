using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using APIName = Bloomberglp.Blpapi.Name;

namespace BBLib.BBEngine
{
    /// <summary>
    /// References BBLib names and sessions
    /// </summary>
    public static class Referential
    {
        // API Names
        internal static readonly APIName BAR_DATA = APIName.GetName("barData");
        internal static readonly APIName BAR_TICK_DATA = APIName.GetName("barTickData");
        internal static readonly APIName BROKER_BUY_CODE = APIName.GetName("brokerBuyCode");
        internal static readonly APIName BROKER_SELL_CODE = APIName.GetName("brokerSellCode");
        internal static readonly APIName CATEGORY = APIName.GetName("category");
        internal static readonly APIName CLOSE = APIName.GetName("close");
        internal static readonly APIName CODE = APIName.GetName("code");
        internal static readonly APIName CONDITION_CODE = APIName.GetName("conditionCode");
        internal static readonly APIName DATE = APIName.GetName("date");
        internal static readonly APIName DESCRIPTION = APIName.GetName("description");
        internal static readonly APIName EID_DATA = APIName.GetName("eidData");
        internal static readonly APIName ERROR_CODE = APIName.GetName("errorCode");
        internal static readonly APIName ERROR_INFO = APIName.GetName("errorInfo");
        internal static readonly APIName EVENT_TYPE = APIName.GetName("eventType");
        internal static readonly APIName EVENT_TYPES = APIName.GetName("eventTypes");
        internal static readonly APIName EXCEPTIONS = APIName.GetName("exceptions");
        internal static readonly APIName EXCHANGE_CODE = APIName.GetName("exchangeCode");
        internal static readonly APIName FIELDS = APIName.GetName("fields");
        internal static readonly APIName FIELD_DATA = APIName.GetName("fieldData");
        internal static readonly APIName FIELD_EXCEPTIONS = APIName.GetName("fieldExceptions");
        internal static readonly APIName FIELD_ID = APIName.GetName("fieldId");
        internal static readonly APIName HIGH = APIName.GetName("high");
        internal static readonly APIName LOW = APIName.GetName("low");
        internal static readonly APIName MARKET_DATA_EVENTS = APIName.GetName("MarketDataEvents");
        internal static readonly APIName MESSAGE = APIName.GetName("message");
        internal static readonly APIName MIC_CODE = APIName.GetName("micCode");
        internal static readonly APIName NUM_EVENTS = APIName.GetName("numEvents");
        internal static readonly APIName OPEN = APIName.GetName("open");
        internal static readonly APIName OVERRIDES = APIName.GetName("overrides");
        internal static readonly APIName REASON = APIName.GetName("reason");
        internal static readonly APIName RELATIVE_DATE = APIName.GetName("relativeDate");
        internal static readonly APIName RESPONSE_ERROR = APIName.GetName("responseError");
        internal static readonly APIName RPS_CODE = APIName.GetName("rpsCode");
        internal static readonly APIName SECURITIES = APIName.GetName("securities");
        internal static readonly APIName SECURITY = APIName.GetName("security");
        internal static readonly APIName SEQUENCE_NUMBER = APIName.GetName("sequenceNumber");
        internal static readonly APIName SECURITY_DATA = APIName.GetName("securityData");
        internal static readonly APIName SECURITY_ERROR = APIName.GetName("securityError");
        internal static readonly APIName SIZE = APIName.GetName("size");
        internal static readonly APIName SOURCE = APIName.GetName("source");
        internal static readonly APIName SUBCATEGORY = APIName.GetName("subcategory");
        internal static readonly APIName TICK_DATA = APIName.GetName("tickData");
        internal static readonly APIName TIME = APIName.GetName("time");
        internal static readonly APIName TYPE = APIName.GetName("type");
        internal static readonly APIName VALUE = APIName.GetName("value");
        internal static readonly APIName VOLUME = APIName.GetName("volume");

        // API Services
        internal const string APIAUTH_SVC = "//blp/apiauth";
        internal const string APIFLDS_SVC = "//blp/apiflds";
        internal const string MKTBAR_SVC = "//blp/mktbar";
        internal const string MKTDATA_SVC = "//blp/mktdata";
        internal const string MKTVWAP_SVC = "//blp/mktvwap";
        internal const string PAGEDATA_SVC = "//blp/pagedata";
        internal const string REFDATA_SVC = "//blp/refdata";
        internal const string TASVC_SVC = "//blp/tasvc";

        // API REFDATA_SVC Requests
        internal const string HSTDATA_REQ = "HistoricalDataRequest";
        internal const string IDTDATA_REQ = "IntradayTickRequest";
        internal const string IDBDATA_REQ = "IntradayBarRequest";
        internal const string REFDATA_REQ = "ReferenceDataRequest";

        // API APIFLDS_SVC Requests
        public const string INFOFLD_REQ = "FieldInfoRequest";

        // Default server address
        internal const string SERVER_HOST = "localhost";
        internal const int SERVER_PORT = 8194;

        // Sessions
        internal static int lastSessionID = 0;
        internal static Dictionary<Session, bool> sessions = new Dictionary<Session, bool>();
        public static Dictionary<Session, bool> Sessions { get { return sessions.ToDictionary(n => n.Key, n => n.Value); } }

        // Response data set names
        internal static readonly string DS_BROKER_BUY_CODE = "BROKER_BUY_CODE";
        internal static readonly string DS_BROKER_SELL_CODE = "BROKER_SELL_CODE";
        internal static readonly string DS_CLOSE = "CLOSE";
        internal static readonly string DS_CONDITION_CODE = "CONDITION_CODE";
        internal static readonly string DS_DATE = "DATE";
        internal static readonly string DS_EXCHANGE_CODE = "EXCHANGE_CODE";
        internal static readonly string DS_HIGH = "HIGH";
        internal static readonly string DS_ID = "ID";
        internal static readonly string DS_LOW = "LOW";
        internal static readonly string DS_MIC_CODE = "MIC_CODE";
        internal static readonly string DS_NUM_EVENTS = "NUM_EVENTS";
        internal static readonly string DS_OPEN = "OPEN";
        internal static readonly string DS_RELATIVE_DATE = "RELATIVE_DATE";
        internal static readonly string DS_RPS_CODE = "RPS_CODE";
        internal static readonly string DS_SIZE = "SIZE";
        internal static readonly string DS_TYPE = "TYPE";
        internal static readonly string DS_VALUE = "VALUE";
        internal static readonly string DS_VOLUME = "VOLUME";

        // Response data set common field index
        internal static readonly Dictionary<string, string> ResponseCommonFieldIndex = new Dictionary<string, string>
        {
            { BROKER_BUY_CODE.ToString(), DS_BROKER_BUY_CODE },
            { BROKER_SELL_CODE.ToString(), DS_BROKER_SELL_CODE },
            { CLOSE.ToString(), DS_CLOSE },
            { CONDITION_CODE.ToString(), DS_CONDITION_CODE },
            { DATE.ToString(), DS_DATE },
            { EXCHANGE_CODE.ToString(), DS_EXCHANGE_CODE },
            { HIGH.ToString(), DS_HIGH },
            { LOW.ToString(), DS_LOW },
            { MIC_CODE.ToString(), DS_MIC_CODE },
            { NUM_EVENTS.ToString(), DS_NUM_EVENTS },
            { OPEN.ToString(), DS_OPEN },
            { RELATIVE_DATE.ToString(), DS_RELATIVE_DATE },
            { RPS_CODE.ToString(), DS_RPS_CODE },
            { SIZE.ToString(), DS_SIZE },
            { TYPE.ToString(), DS_TYPE },
            { VALUE.ToString(), DS_VALUE },
            { VOLUME.ToString(), DS_VOLUME }
        };
    }
}