CREATE DATABASE IF NOT EXISTS telemetry_demo;

INSTALL COMPONENT 'file://component_query_attributes';
INSTALL COMPONENT 'file://component_telemetry';

SET PERSIST_ONLY telemetry.trace_enabled = ON;
SET PERSIST_ONLY telemetry.query_text_enabled = ON;
SET PERSIST_ONLY telemetry.otel_exporter_otlp_traces_endpoint = 'http://host.docker.internal:4318/v1/traces';
SET PERSIST_ONLY telemetry.otel_exporter_otlp_traces_protocol = 'http/protobuf';
SET PERSIST_ONLY telemetry.otel_exporter_otlp_metrics_endpoint = 'http://host.docker.internal:4318/v1/metrics';
SET PERSIST_ONLY telemetry.otel_exporter_otlp_metrics_protocol = 'http/protobuf';
SET PERSIST_ONLY telemetry.otel_exporter_otlp_logs_endpoint = 'http://host.docker.internal:4318/v1/logs';
SET PERSIST_ONLY telemetry.otel_exporter_otlp_logs_protocol = 'http/protobuf';
SET PERSIST_ONLY telemetry.otel_resource_attributes = 'service.name=mysql-telemetry-demo';
