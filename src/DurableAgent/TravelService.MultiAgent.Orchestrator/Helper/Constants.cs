using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelService.MultiAgent.Orchestrator.Helper
{
   public class OpenTelemetryConstants
   {
      // CONSTANTS FOR MESSAGE BROKER OTEL STRINGS 
      public const string MESSAGING_SYSTEM_KEY = "messaging.system";
      public const string MESSAGING_SYSTEM_VALUE = "kafka";
      public const string MESSAGING_DESTINATION_KEY = "messaging.destination";
      public const string MESSAGING_DESTINATION_KIND_KEY = "messaging.destination.kind";
      public const string MESSAGING_DESTINATION_KIND_VALUE = "topic";
      public const string MESSAGING_PROTOCOL_KEY = "messaging.protocol";
      public const string MESSAGING_PROTOCOL_VALUE = "kafka";
      public const string MESSAGING_URL_KEY = "messaging.url";
      public const string MESSAGING_ID_KEY = "messaging.message_id";


      // CONSTANTS FOR ORCHESTRATOR TRIGGER OTEL STRINGS
      public const string ORCHESTRATOR_TRIGGER_KEY = "orchestrator.trigger";
      public const string ORCHESTRATOR_TRIGGER_VALUE = "orchestrator";
      public const string ORCHESTRATOR_NAME_KEY = "orchestrator.name";
      public const string ORCHESTRATOR_INSTANCE_ID_KEY = "orchestrator.instance_id";
      public const string ORCHESTRATOR_START_TIME_KEY = "orchestrator.start_time";
      public const string ORCHESTRATOR_END_TIME_KEY = "orchestrator.end_time";


      // CONSTANTS FOR ACTIVITY TRIGGER OTEL STRINGS
      public const string ACTIVITY_TRIGGER_KEY = "activity.trigger";
      public const string ACTIVITY_TRIGGER_VALUE = "activity";
      public const string ACTIVITY_NAME_KEY = "activity.name";
      public const string ACTIVITY_INSTANCE_ID_KEY = "activity.instance_id";
      public const string ACTIVITY_START_TIME_KEY = "activity.start_time";
      public const string ACTIVITY_END_TIME_KEY = "activity.end_time";


      // CONSTANTS FOR DATABASE OTEL STRINGS
      public const string DATABASE_SYSTEM_KEY = "db.system";
      public const string DATABASE_SYSTEM_VALUE = "cosmosdb";
      public const string NET_PEER_NAME_KEY = "net.peer.name";
      public const string NET_PEER_PORT_KEY = "net.peer.port";
      public const string NET_PEER_PORT_VALUE = "443";
      public const string DATABASE_NAME_KEY = "db.name";
      public const string DATABASE_OPERATION_KEY = "db.operation";
      public const string DATABASE_COLLECTION_NAME_KEY = $"db.{DATABASE_SYSTEM_VALUE}.collection";


      // CONSTANTS FOR OTEL GENERAL ATTRIBUTES
      public const string TRACEID_KEY = "traceId";
      public const string PARENT_SPANID_KEY = "parentSpanId";
      public const string PARENT_SPAN_TRACEFLAG_KEY = "parentSpanTraceFlag";
      public const string OPERATION_STARTTIME_KEY = "StartTime";
      public const string OPERATION_ENDTIME_KEY = "EndTime";

      // REQUEST and RESPONSE CONSTANTS
      public const string REQUEST_DATA_KEY = "RequestData";
      public const string RESPONSE_DATA_KEY = "ResponseData";
      public const string TOKEN_CONSUMPION_KEY = "TokenConsumption";
   }
}
