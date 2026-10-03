using AgentFlow.Api.Agents;
using AgentFlow.Api.Models;
using AgentFlow.Api.Services;

namespace AgentFlow.Api.Seeding;

public static class SampleWorkflows
{
    public static async Task SeedAsync(WorkflowRepository repo, AgentRegistry registry)
    {
        var agents = new[]
        {
            new AgentRecord("data-extractor", "PostgreSQL Extractor", "data-extractor",
                "Read-only SQL query executor against the QueryNex DB", new(), DateTime.UtcNow),
            new AgentRecord("analyzer", "Business Analyzer", "analyzer",
                "LLM data analysis", new() {
                    ["system"] = "You are a senior data analyst. Summarize key trends and outliers. Use 3-5 bullets."
                }, DateTime.UtcNow),
            new AgentRecord("report-writer", "Executive Report Writer", "report-writer",
                "Markdown report generation", new(), DateTime.UtcNow),
            new AgentRecord("email-sender", "Email Notifier", "email-sender",
                "Logs (demo) email dispatch", new(), DateTime.UtcNow),
            new AgentRecord("http", "HTTP Caller", "http",
                "Generic HTTP agent", new(), DateTime.UtcNow),
        };
        foreach (var a in agents) { await repo.UpsertAgentAsync(a); registry.RegisterRecord(a); }

        // 1. Monthly Sales Report — matches the real QueryNex schema
        var monthly = new WorkflowDefinitionJson(
            "monthly-sales-report",
            "Monthly Sales Report",
            "Extracts 90-day category sales, analyzes, writes report, requires approval, then emails.",
            new List<StepDefinitionJson>
            {
                new("extract", "Extract 90-day category sales", "data-extractor", null, null,
                    new() { ["query"] = """
                        SELECT c.name                              AS category,
                               SUM(oi.quantity * oi.unit_price)    AS revenue,
                               SUM(oi.quantity)                    AS units
                        FROM order_items oi
                        JOIN products   p ON p.product_id  = oi.product_id
                        JOIN categories c ON c.category_id = p.category_id
                        JOIN orders     o ON o.order_id    = oi.order_id
                        WHERE o.status = 'delivered'
                          AND o.order_date >= CURRENT_DATE - INTERVAL '90 days'
                        GROUP BY c.name
                        ORDER BY revenue DESC;
                        """ },
                    new RetryJson(2, 500), 60, null, null),

                new("analyze", "Analyze category trends", "analyzer", null, new(){ "extract" },
                    new() { ["data"] = "${extract.output.rows}" },
                    new RetryJson(2, 1000), 120, null, null),

                new("write", "Write report", "report-writer", null, new(){ "analyze" },
                    new() { ["analysis"] = "${analyze.output.text}", ["tone"] = "executive", ["title"] = "Quarterly Sales Report" },
                    new RetryJson(2, 1000), 120, null, null),

                new("approve", "Manager approval", null, "approval", new(){ "write" },
                    null, null, null, new(){ "manager" }, null),

                new("send", "Email the report", "email-sender", null, new(){ "approve" },
                    new() { ["to"] = "team@querynex.ai", ["subject"] = "Quarterly Sales Report", ["body"] = "${write.output.markdown}" },
                    new RetryJson(3, 500), 30, null, null),
            });

        // 2. Cross-System Daily Brief — uses invoices + MVs + KB from other QueryNex projects
        var crossSystem = new WorkflowDefinitionJson(
            "cross-system-daily-brief",
            "Cross-System Daily Brief",
            "Pulls open invoices, top products (MV), and KB stats in parallel, then composes one ops brief.",
            new List<StepDefinitionJson>
            {
                new("open_invoices", "Open invoices >30d", "data-extractor", null, null,
                    new() { ["query"] = """
                        SELECT vendor_name, invoice_number, due_date, total_amount
                        FROM invoices
                        WHERE status IN ('pending','needs_review')
                          AND due_date < CURRENT_DATE
                        ORDER BY due_date;
                        """ },
                    new RetryJson(2, 500), 60, null, null),

                new("top_products", "Top products (MV)", "data-extractor", null, null,
                    new() { ["query"] = "SELECT * FROM mv_top_products LIMIT 10;" },
                    new RetryJson(2, 500), 60, null, null),

                new("kb_stats", "Knowledge base stats", "data-extractor", null, null,
                    new() { ["query"] = """
                        SELECT d.source_type,
                               COUNT(DISTINCT d.document_id) AS docs,
                               COUNT(c.chunk_id)             AS chunks
                        FROM documents d
                        LEFT JOIN document_chunks c ON c.document_id = d.document_id
                        GROUP BY d.source_type;
                        """ },
                    new RetryJson(2, 500), 60, null, null),

                new("brief", "Compose ops brief", "analyzer", null,
                    new(){ "open_invoices", "top_products", "kb_stats" },
                    new() { ["system"] = "You are an ops lead. Combine the three datasets into a tight daily brief: what's overdue, what's selling, what's missing in the KB.",
                            ["data"]   = new Dictionary<string, object?>
                            {
                                ["overdueInvoices"] = "${open_invoices.output.rows}",
                                ["topProducts"]     = "${top_products.output.rows}",
                                ["kbCoverage"]      = "${kb_stats.output.rows}"
                            } },
                    new RetryJson(2, 1000), 120, null, null),

                new("deliver", "Deliver brief", "email-sender", null, new(){ "brief" },
                    new() { ["to"] = "ops@querynex.ai", ["subject"] = "Daily Ops Brief", ["body"] = "${brief.output.text}" },
                    new RetryJson(3, 500), 30, null, null),
            });

        await repo.UpsertWorkflowAsync(monthly);
        await repo.UpsertWorkflowAsync(crossSystem);
    }
}
