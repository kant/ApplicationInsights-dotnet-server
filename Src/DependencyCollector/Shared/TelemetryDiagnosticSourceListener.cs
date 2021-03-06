namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    internal class TelemetryDiagnosticSourceListener : DiagnosticSourceListenerBase<HashSet<string>>
    {
        internal const string ActivityStartNameSuffix = ".Start";
        internal const string ActivityStopNameSuffix = ".Stop";

        private readonly HashSet<string> includedDiagnosticSources 
            = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, HashSet<string>> includedDiagnosticSourceActivities 
            = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        public TelemetryDiagnosticSourceListener(TelemetryConfiguration configuration, ICollection<string> includeDiagnosticSourceActivities) 
            : base(configuration)
        {
            this.Client.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion("rdd" + RddSource.DiagnosticSourceListener + ":");
            this.PrepareInclusionLists(includeDiagnosticSourceActivities);
        }

        internal override bool IsSourceEnabled(DiagnosticListener value)
        {
            return this.includedDiagnosticSources.Contains(value.Name);
        }

        internal override bool IsEventEnabled(string evnt, object input1, object input2, DiagnosticListener diagnosticListener, HashSet<string> context)
        {
            return this.IsActivityIncluded(evnt, context)
                && !evnt.EndsWith(ActivityStartNameSuffix, StringComparison.OrdinalIgnoreCase);
        }

        internal bool IsActivityIncluded(string activityName, HashSet<string> includedActivities)
        {
            // if no list of included activities then all are included
            return includedActivities == null || includedActivities.Contains(activityName);
        }

        internal override void HandleEvent(KeyValuePair<string, object> evnt, DiagnosticListener diagnosticListener, HashSet<string> context)
        {
            if (!this.IsActivityIncluded(evnt.Key, context)
                || !evnt.Key.EndsWith(ActivityStopNameSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Activity currentActivity = Activity.Current;
            if (currentActivity == null)
            {
                DependencyCollectorEventSource.Log.CurrentActivityIsNull();
                return;
            }

            DependencyCollectorEventSource.Log.TelemetryDiagnosticSourceListenerActivityStopped(currentActivity.Id, currentActivity.OperationName);

            // extensibility point - can chain more telemetry extraction methods here
            ITelemetry telemetry = this.ExtractDependencyTelemetry(diagnosticListener, currentActivity);
            if (telemetry == null)
            {
                return;
            }

            // properly fill dependency telemetry operation context
            telemetry.Context.Operation.Id = currentActivity.RootId;
            telemetry.Context.Operation.ParentId = currentActivity.ParentId;
            telemetry.Timestamp = currentActivity.StartTimeUtc;

            telemetry.Context.Properties["DiagnosticSource"] = diagnosticListener.Name;
            telemetry.Context.Properties["Activity"] = currentActivity.OperationName;

            this.Client.Track(telemetry);
        }

        internal DependencyTelemetry ExtractDependencyTelemetry(DiagnosticListener diagnosticListener, Activity currentActivity)
        {
            DependencyTelemetry telemetry = new DependencyTelemetry();

            telemetry.Id = currentActivity.Id;
            telemetry.Duration = currentActivity.Duration;
            telemetry.Name = currentActivity.OperationName;

            Uri requestUri = null;
            string component = null;
            string queryStatement = null;
            string httpMethodWithSpace = string.Empty;
            string httpUrl = null;
            string peerAddress = null;
            string peerService = null;

            foreach (KeyValuePair<string, string> tag in currentActivity.Tags)
            {
                // interpret Tags as defined by OpenTracing conventions
                // https://github.com/opentracing/specification/blob/master/semantic_conventions.md
                switch (tag.Key)
                {
                    case "component":
                        {
                            component = tag.Value;
                            break;
                        }

                    case "db.statement":
                        {
                            queryStatement = tag.Value;
                            break;
                        }

                    case "error":
                        {
                            bool failed;
                            if (bool.TryParse(tag.Value, out failed))
                            {
                                telemetry.Success = !failed;
                                continue; // skip Properties
                            }

                            break;
                        }

                    case "http.status_code":
                        {
                            telemetry.ResultCode = tag.Value;
                            continue; // skip Properties
                        }

                    case "http.method":
                        {
                            httpMethodWithSpace = tag.Value + " ";
                            break;
                        }

                    case "http.url":
                        {
                            httpUrl = tag.Value;
                            if (Uri.TryCreate(tag.Value, UriKind.RelativeOrAbsolute, out requestUri))
                            {
                                continue; // skip Properties
                            }

                            break;
                        }

                    case "peer.address":
                        {
                            peerAddress = tag.Value;
                            break;
                        }

                    case "peer.hostname":
                        {
                            telemetry.Target = tag.Value;
                            continue; // skip Properties
                        }

                    case "peer.service":
                        {
                            peerService = tag.Value;
                            break;
                        }
                }

                // if more than one tag with the same name is specified, the first one wins
                // TODO verify if still needed once https://github.com/Microsoft/ApplicationInsights-dotnet/issues/562 is resolved 
                if (!telemetry.Context.Properties.ContainsKey(tag.Key))
                {
                    telemetry.Context.Properties.Add(tag);
                }
            }

            if (string.IsNullOrEmpty(telemetry.Type))
            {
                telemetry.Type = peerService ?? component ?? diagnosticListener.Name;
            }

            if (string.IsNullOrEmpty(telemetry.Target))
            {
                // 'peer.address' can be not user-friendly, thus use only if nothing else specified
                telemetry.Target = requestUri?.Host ?? peerAddress;
            }

            if (string.IsNullOrEmpty(telemetry.Name))
            {
                telemetry.Name = currentActivity.OperationName;
            }

            if (string.IsNullOrEmpty(telemetry.Data))
            {
                telemetry.Data = queryStatement ?? requestUri?.OriginalString ?? httpUrl;
            }

            return telemetry;
        }

        protected override HashSet<string> GetListenerContext(DiagnosticListener diagnosticListener)
        {
            HashSet<string> includedActivities;
            if (!this.includedDiagnosticSourceActivities.TryGetValue(diagnosticListener.Name, out includedActivities))
            {
                return null;
            }

            return includedActivities;
        }

        private void PrepareInclusionLists(ICollection<string> includeDiagnosticSourceActivities)
        {
            if (includeDiagnosticSourceActivities == null)
            {
                return;
            }

            foreach (string inclusion in includeDiagnosticSourceActivities)
            {
                if (string.IsNullOrWhiteSpace(inclusion))
                {
                    continue;
                }

                // each individual inclusion can specify
                // 1) the name of Diagnostic Source 
                //    - in that case the whole source is included
                //    - e.g. "System.Net.Http"
                // 2) the names of Diagnostic Source and Activity separated by ':' 
                //   - in that case only the activity is enabled from given source
                //   - e.g. ""
                string[] tokens = inclusion.Split(':');

                // the Diagnostic Source is included (even if only certain activities are enabled)
                this.includedDiagnosticSources.Add(tokens[0]);

                if (tokens.Length > 1)
                {
                    // only certain Activity from the Diagnostic Source is included
                    HashSet<string> includedActivities;
                    if (!this.includedDiagnosticSourceActivities.TryGetValue(tokens[0], out includedActivities))
                    {
                        includedActivities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        this.includedDiagnosticSourceActivities[tokens[0]] = includedActivities;
                    }

                    // include activity and activity Stop events
                    includedActivities.Add(tokens[1]);
                    includedActivities.Add(tokens[1] + ActivityStopNameSuffix);
                }
            }
        }
    }
}