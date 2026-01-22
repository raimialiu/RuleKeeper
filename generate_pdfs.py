#!/usr/bin/env python3
"""
RuleKeeper PDF Generator
Generates two PDF documents:
1. Quick Reference Lookup - Complete list of all policies
2. Detailed Policy Guide - Full descriptions with examples
"""

from fpdf import FPDF
from fpdf.enums import XPos, YPos
from pathlib import Path


# =============================================================================
# POLICY DATA
# =============================================================================
CATEGORIES = [
    ("naming_conventions", "Naming Conventions", [
        ("CS-NAME-001", "Class/Interface Naming", "Classes and Interfaces must use PascalCase", "high"),
        ("CS-NAME-002", "Method Naming", "Methods must use PascalCase", "high"),
        ("CS-NAME-003", "Variable/Field/Parameter Naming", "Variables, fields, and parameters must use camelCase", "high"),
        ("CS-NAME-004", "Constant Naming", "Constants must use UPPER_SNAKE_CASE", "high"),
        ("CS-NAME-005", "Private Field Naming", "Private fields must use _camelCase (underscore prefix)", "high"),
        ("CS-NAME-006", "Async Method Naming", "Async methods must end with 'Async' suffix", "high"),
        ("CS-NAME-007", "Interface Naming", "Interfaces must be prefixed with 'I'", "high"),
        ("CS-NAME-008", "Request/Response DTO Naming", "Request and Response DTOs must end with Request or Response suffix", "high"),
        ("CS-NAME-009", "Boolean Variable Naming", "Boolean variables should use is/has/can/should prefixes", "medium"),
        ("CS-NAME-010", "Event Handler Naming", "Event handlers should follow 'On' + EventName pattern", "medium"),
    ]),
    ("file_organization", "File & Project Organization", [
        ("CS-FILE-001", "One Class Per File", "Each file should contain only one class", "medium"),
        ("CS-FILE-002", "File Name Matches Class", "File name must match the class name it contains", "high"),
        ("CS-FILE-003", "Feature-Based Organization", "Group files logically by feature, not layer (vertical slicing)", "medium"),
        ("CS-FILE-004", "Namespace Matches Folder Structure", "Namespace should reflect the folder structure", "medium"),
    ]),
    ("method_design", "Method Design & Readability", [
        ("CS-METHOD-001", "Single Responsibility", "Methods should be small and do one thing", "high"),
        ("CS-METHOD-002", "Method Length", "Keep method length at or below 30 lines", "medium"),
        ("CS-METHOD-003", "Parameter Count", "Avoid long parameter lists - use DTOs instead", "medium"),
        ("CS-METHOD-004", "Cyclomatic Complexity", "Methods should have low cyclomatic complexity", "medium"),
        ("CS-METHOD-005", "No Nested Ternary", "Avoid nested ternary operators", "medium"),
    ]),
    ("secure_coding", "Secure Coding Practices", [
        ("CS-SEC-001", "Parameterized Queries", "Never concatenate SQL or user inputs - use parameterized queries", "critical"),
        ("CS-SEC-002", "Input Validation", "Always validate user input", "critical"),
        ("CS-SEC-003", "Log Sanitization", "Sanitize logs - no sensitive data (PIN, password, token)", "critical"),
        ("CS-SEC-004", "Secret Protection", "Use SecureString or data masking for secrets", "critical"),
        ("CS-SEC-005", "Configuration Security", "Protect configuration via Azure Key Vault or AWS Secrets Manager", "critical"),
        ("CS-SEC-006", "No Hardcoded Credentials", "Never hardcode credentials in source code", "critical"),
        ("CS-SEC-007", "XSS Prevention", "Sanitize output to prevent Cross-Site Scripting", "critical"),
        ("CS-SEC-008", "Path Traversal Prevention", "Validate file paths to prevent directory traversal", "critical"),
    ]),
    ("exception_handling", "Exception Handling & Logging", [
        ("CS-EXC-001", "Meaningful Exception Handling", "Use try-catch only where you can handle errors meaningfully", "high"),
        ("CS-EXC-002", "Contextual Logging", "Log exceptions with context, but not sensitive data", "high"),
        ("CS-EXC-003", "No Empty Catch Blocks", "Avoid empty catch blocks", "critical"),
        ("CS-EXC-004", "Domain Exceptions", "Throw domain-specific exceptions when needed", "medium"),
        ("CS-EXC-005", "No Catch-All Without Rethrow", "Catching all exceptions should rethrow or terminate", "high"),
    ]),
    ("async_programming", "Asynchronous Programming", [
        ("CS-ASYNC-001", "Always Await", "Always await async calls", "high"),
        ("CS-ASYNC-002", "No Blocking Async", "Don't block async with .Result or .Wait()", "critical"),
        ("CS-ASYNC-003", "ConfigureAwait in Libraries", "Use ConfigureAwait(false) in library code", "medium"),
        ("CS-ASYNC-004", "Async Void Avoidance", "Avoid async void except for event handlers", "high"),
        ("CS-ASYNC-005", "Proper Cancellation Token Usage", "Async methods should accept and use CancellationToken", "medium"),
    ]),
    ("dependency_injection", "Dependency Injection & SOLID", [
        ("CS-DI-001", "Depend on Abstractions", "Depend on interfaces, not concrete types", "high"),
        ("CS-DI-002", "Use IoC Container", "Use built-in IServiceCollection or IoC containers", "high"),
        ("CS-DI-003", "Avoid New in Business Logic", "Avoid 'new' keyword for dependencies inside business logic", "high"),
        ("CS-DI-004", "Constructor Injection Only", "Use constructor injection, not property or method injection", "medium"),
        ("CS-DI-005", "Service Lifetime Consistency", "Ensure consistent service lifetimes in DI registration", "high"),
    ]),
    ("constants", "Constants & Magic Numbers", [
        ("CS-CONST-001", "No Magic Numbers", "Avoid magic numbers or strings in code", "medium"),
        ("CS-CONST-002", "Use Named Constants", "Use named constants or enums instead of literals", "medium"),
        ("CS-CONST-003", "No Magic Strings", "Avoid magic strings in code", "medium"),
    ]),
    ("data_validation", "Data Validation", [
        ("CS-VAL-001", "DTO Validation", "Always validate input DTOs using attributes or FluentValidation", "high"),
        ("CS-VAL-002", "Client and Server Validation", "Validate both client and server side", "high"),
        ("CS-VAL-003", "Null Checks", "Check for null before using objects", "high"),
        ("CS-VAL-004", "Guard Clauses", "Use guard clauses for parameter validation", "medium"),
    ]),
    ("logging", "Logging Standards", [
        ("CS-LOG-001", "Structured Logging", "Use structured logging", "high"),
        ("CS-LOG-002", "No Sensitive Data in Logs", "Never log sensitive data (PIN, password, token)", "critical"),
        ("CS-LOG-003", "Appropriate Log Levels", "Log at appropriate levels (Info, Warning, Error, Critical)", "medium"),
        ("CS-LOG-004", "Include Correlation ID", "Include correlation/trace ID in logs for distributed tracing", "medium"),
    ]),
    ("documentation", "Code Comments & Documentation", [
        ("CS-DOC-001", "XML Comments for Public APIs", "Use XML comments for public APIs", "medium"),
        ("CS-DOC-002", "Comment Why Not What", "Comment why, not what - avoid redundant comments", "low"),
        ("CS-DOC-003", "TODO Comments", "TODO comments should include ticket/issue reference", "low"),
    ]),
    ("immutability", "Immutability & Defensive Coding", [
        ("CS-IMM-001", "Use Readonly", "Use readonly for fields that don't change after construction", "medium"),
        ("CS-IMM-002", "No Mutable Collections", "Avoid exposing mutable collections", "medium"),
        ("CS-IMM-003", "Clone External Data", "Clone or copy external data inputs", "medium"),
        ("CS-IMM-004", "Use Records for DTOs", "Consider using records for immutable DTOs", "low"),
    ]),
    ("secure_configuration", "Secure Configuration", [
        ("CS-CFG-001", "No Secrets in Source", "No secrets in source code or appsettings.json", "critical"),
        ("CS-CFG-002", "Use Secret Managers", "Use environment variables or secret managers", "critical"),
        ("CS-CFG-003", "Secure Connection Strings", "Connection strings should use integrated security or managed identity", "high"),
    ]),
    ("secure_strings", "Secure String Handling", [
        ("CS-STR-001", "No Plain Text Secrets", "Avoid keeping secrets as plain strings in memory", "high"),
        ("CS-STR-002", "Use SecureString", "Use SecureString or encrypt secrets in memory", "high"),
    ]),
    ("unit_testing", "Unit Testing Standards", [
        ("CS-TEST-001", "Test Naming Convention", "Use clear test names: MethodName_StateUnderTest_ExpectedBehavior", "medium"),
        ("CS-TEST-002", "Single Assertion", "Prefer one logical assertion per test", "low"),
        ("CS-TEST-003", "No External Dependencies", "No dependency on external systems in unit tests", "high"),
        ("CS-TEST-004", "Arrange-Act-Assert Pattern", "Tests should follow Arrange-Act-Assert pattern", "low"),
        ("CS-TEST-005", "Test Class Naming", "Test classes should be named {ClassName}Tests", "low"),
    ]),
    ("cors", "CORS Configuration", [
        ("API-CORS-001", "Specific CORS Origins", "Configure CORS with specific allowed origins, not AllowAnyOrigin", "critical"),
        ("API-CORS-002", "No Credentials with Any Origin", "AllowCredentials cannot be used with AllowAnyOrigin", "critical"),
    ]),
    ("api_design", "API Design", [
        ("API-REST-001", "RESTful Endpoints", "Use proper HTTP methods for CRUD operations", "high"),
        ("API-HTTP-001", "Appropriate Status Codes", "Return appropriate HTTP status codes", "high"),
        ("API-VER-001", "API Versioning", "Implement API versioning in routes", "high"),
        ("API-RESP-001", "Consistent Response Format", "Use a consistent API response wrapper", "medium"),
        ("API-DOC-001", "Endpoint Documentation", "Document API endpoints with XML comments and response types", "medium"),
    ]),
    ("encryption", "Encryption", [
        ("API-ENC-001", "Proper RSA Encryption", "Use proper RSA encryption with OAEP padding", "critical"),
        ("API-ENC-002", "Strong Hashing Algorithms", "Use SHA-256 or stronger for hashing", "critical"),
    ]),
    ("idempotency", "Idempotency", [
        ("API-IDEMP-001", "Idempotency Keys", "Use idempotency keys for financial operations", "critical"),
    ]),
    ("authentication", "Authentication & Authorization", [
        ("API-AUTH-001", "Endpoint Authorization", "Protect endpoints with proper authorization", "critical"),
        ("API-AUTH-002", "Resource-Level Authorization", "Verify user has access to specific resources", "critical"),
    ]),
    ("error_handling", "Error Handling", [
        ("API-ERR-001", "Domain Exception Handling", "Handle domain-specific exceptions with appropriate responses", "high"),
        ("API-SAN-001", "Input Sanitization", "Sanitize all user inputs before processing", "critical"),
    ]),
    ("rate_limiting", "Rate Limiting", [
        ("API-RATE-001", "Rate Limiting", "Implement rate limiting on API endpoints", "high"),
    ]),
]

# Detailed examples for the detailed guide
DETAILED_RULES = {
    "CS-NAME-001": {
        "pattern": "^[A-Z][a-zA-Z0-9]*$",
        "good": "AccountService, IAccountRepository, TransactionHandler",
        "bad": "accountservice, account_service, iAccountRepo",
        "fix_hint": "Rename to start with uppercase letter, e.g., 'AccountService'",
    },
    "CS-NAME-002": {
        "pattern": "^[A-Z][a-zA-Z0-9]*$",
        "good": "GetAccountBalance, ProcessPayment, ValidateInput",
        "bad": "getbalance, process_payment, validateInput",
        "fix_hint": "Rename to start with uppercase letter",
    },
    "CS-NAME-003": {
        "pattern": "^[a-z][a-zA-Z0-9]*$",
        "good": "accountId, transactionAmount, userName",
        "bad": "AccountId, transaction_amount, UserName",
        "fix_hint": "Rename to start with lowercase letter",
    },
    "CS-NAME-004": {
        "pattern": "^[A-Z][A-Z0-9_]*$",
        "good": "MAX_RETRY_COUNT, DEFAULT_TIMEOUT, API_VERSION",
        "bad": "maxRetryCount, defaultTimeout, ApiVersion",
        "fix_hint": "Rename using uppercase with underscores",
    },
    "CS-NAME-005": {
        "pattern": "^_[a-z][a-zA-Z0-9]*$",
        "good": "_accountRepository, _logger, _transactionService",
        "bad": "AccountRepo, accountRepository, _AccountRepo",
        "fix_hint": "Rename with underscore prefix and lowercase",
    },
    "CS-NAME-006": {
        "pattern": ".*Async$",
        "good": "GetAccountBalanceAsync, ProcessPaymentAsync",
        "bad": "GetAccountBalance (for async methods)",
        "fix_hint": "Add 'Async' suffix to async method names",
    },
    "CS-NAME-007": {
        "pattern": "^I[A-Z][a-zA-Z0-9]*$",
        "good": "IAccountService, ITransactionRepository, ILogger",
        "bad": "AccountServiceInterface, AccountService (for interfaces)",
        "fix_hint": "Add 'I' prefix to interface names",
    },
    "CS-NAME-008": {
        "pattern": "^[A-Z][a-zA-Z0-9]*(Request|Response)$",
        "good": "TransferRequest, AccountResponse, LoginRequest",
        "bad": "TransferDTO, AccountData, LoginPayload",
        "fix_hint": "Add 'Request' or 'Response' suffix",
    },
    "CS-SEC-001": {
        "pattern": None,
        "good": "cmd.Parameters.AddWithValue(\"@id\", accountId);",
        "bad": "\"SELECT * FROM Accounts WHERE Id = '\" + accountId + \"'\"",
        "fix_hint": "Use parameterized queries with @parameters",
    },
    "CS-SEC-003": {
        "pattern": None,
        "good": "_logger.LogInformation(\"Transfer for {AccountId}\", accountId);",
        "bad": "_logger.LogInformation($\"Login with PIN {pin}\");",
        "fix_hint": "Remove sensitive data from log statements",
    },
    "CS-ASYNC-002": {
        "pattern": None,
        "good": "var result = await _service.GetAsync();",
        "bad": "var result = _service.GetAsync().Result;",
        "fix_hint": "Use 'await' instead of .Result or .Wait()",
    },
    "CS-EXC-003": {
        "pattern": None,
        "good": "catch (Exception ex) { _logger.LogError(ex, \"Error\"); throw; }",
        "bad": "catch (Exception) { /* ignore */ }",
        "fix_hint": "Log the exception or handle it meaningfully",
    },
    "CS-DI-001": {
        "pattern": None,
        "good": "private readonly ITransactionService _transactionService;",
        "bad": "private readonly TransactionService _transactionService;",
        "fix_hint": "Change type to interface",
    },
    "API-CORS-001": {
        "pattern": None,
        "good": "policy.WithOrigins(\"https://example.com\").AllowCredentials();",
        "bad": "policy.AllowAnyOrigin().AllowAnyMethod();",
        "fix_hint": "Specify allowed origins with WithOrigins()",
    },
    "API-AUTH-001": {
        "pattern": None,
        "good": "[Authorize(Policy = \"BankingCustomer\")]",
        "bad": "[HttpGet(\"accounts\")] // no authorization",
        "fix_hint": "Add [Authorize] attribute to secure endpoints",
    },
}


# =============================================================================
# PDF 1: QUICK REFERENCE LOOKUP
# =============================================================================
class QuickReferencePDF(FPDF):
    def __init__(self):
        super().__init__()
        self.set_auto_page_break(auto=True, margin=15)

    def header(self):
        if self.page_no() > 1:
            self.set_font('Helvetica', 'B', 9)
            self.set_text_color(100, 100, 100)
            self.cell(0, 8, 'RuleKeeper - Quick Reference', new_x=XPos.LMARGIN, new_y=YPos.NEXT, align='C')
            self.ln(2)

    def footer(self):
        self.set_y(-15)
        self.set_font('Helvetica', 'I', 8)
        self.set_text_color(128)
        self.cell(0, 10, f'Page {self.page_no()}/{{nb}}', align='C')


def generate_quick_reference():
    pdf = QuickReferencePDF()
    pdf.alias_nb_pages()

    # Title Page
    pdf.add_page()
    pdf.set_font('Helvetica', 'B', 32)
    pdf.set_text_color(0, 51, 102)
    pdf.ln(50)
    pdf.cell(0, 15, 'RuleKeeper', align='C', new_x=XPos.LMARGIN, new_y=YPos.NEXT)

    pdf.set_font('Helvetica', '', 18)
    pdf.set_text_color(100, 100, 100)
    pdf.cell(0, 10, 'Policy Quick Reference', align='C', new_x=XPos.LMARGIN, new_y=YPos.NEXT)

    pdf.ln(10)
    pdf.set_font('Helvetica', 'I', 12)
    pdf.cell(0, 8, 'Complete Policy Lookup Guide', align='C', new_x=XPos.LMARGIN, new_y=YPos.NEXT)

    pdf.ln(30)
    pdf.set_font('Helvetica', '', 10)
    pdf.set_text_color(0)
    pdf.multi_cell(0, 6,
        "This document provides a quick reference lookup for all RuleKeeper policies. "
        "Use this guide when modifying the rulekeeper.yaml configuration file.", align='C')

    pdf.ln(20)

    # Severity Legend
    pdf.set_font('Helvetica', 'B', 11)
    pdf.cell(0, 8, 'Severity Levels:', new_x=XPos.LMARGIN, new_y=YPos.NEXT)
    pdf.ln(2)

    severities = [
        ("CRITICAL", (220, 53, 69), "Block deployment - Security/compliance risk"),
        ("HIGH", (255, 153, 0), "Fix before deployment"),
        ("MEDIUM", (255, 193, 7), "Address in current sprint"),
        ("LOW", (40, 167, 69), "Best practice recommendation"),
    ]

    for name, color, desc in severities:
        pdf.set_fill_color(*color)
        pdf.set_text_color(255)
        pdf.set_font('Helvetica', 'B', 9)
        pdf.cell(25, 6, name, fill=True)
        pdf.set_text_color(0)
        pdf.set_font('Helvetica', '', 9)
        pdf.cell(0, 6, f"  {desc}", new_x=XPos.LMARGIN, new_y=YPos.NEXT)

    # Policy Tables
    for cat_id, cat_name, rules in CATEGORIES:
        pdf.add_page()
        pdf.set_font('Helvetica', 'B', 14)
        pdf.set_text_color(0, 51, 102)
        pdf.cell(0, 10, cat_name, new_x=XPos.LMARGIN, new_y=YPos.NEXT)
        pdf.ln(2)

        # Table Header
        pdf.set_fill_color(240, 240, 240)
        pdf.set_font('Helvetica', 'B', 8)
        pdf.set_text_color(0)
        pdf.cell(28, 7, 'ID', border=1, fill=True)
        pdf.cell(45, 7, 'Name', border=1, fill=True)
        pdf.cell(100, 7, 'Description', border=1, fill=True)
        pdf.cell(17, 7, 'Severity', border=1, fill=True, new_x=XPos.LMARGIN, new_y=YPos.NEXT)

        # Table Rows
        pdf.set_font('Helvetica', '', 7)
        for rule_id, name, desc, severity in rules:
            # Severity color
            severity_colors = {
                'critical': (255, 230, 230),
                'high': (255, 243, 224),
                'medium': (255, 249, 219),
                'low': (232, 245, 233)
            }
            pdf.set_fill_color(*severity_colors.get(severity, (255, 255, 255)))

            # Truncate if needed
            name_short = name[:25] + "..." if len(name) > 28 else name
            desc_short = desc[:60] + "..." if len(desc) > 63 else desc

            pdf.cell(28, 6, rule_id, border=1, fill=True)
            pdf.cell(45, 6, name_short, border=1, fill=True)
            pdf.cell(100, 6, desc_short, border=1, fill=True)
            pdf.cell(17, 6, severity.upper()[:4], border=1, fill=True, align='C',
                     new_x=XPos.LMARGIN, new_y=YPos.NEXT)

    # YAML Configuration Options Page
    pdf.add_page()
    pdf.set_font('Helvetica', 'B', 14)
    pdf.set_text_color(0, 51, 102)
    pdf.cell(0, 10, 'YAML Configuration Options', new_x=XPos.LMARGIN, new_y=YPos.NEXT)
    pdf.ln(2)

    options = [
        ("id", "string", "Unique identifier for the rule"),
        ("name", "string", "Human-readable rule name"),
        ("description", "string", "Detailed description of what the rule enforces"),
        ("severity", "critical|high|medium|low", "Severity level of violations"),
        ("enabled", "true|false", "Whether the rule is active"),
        ("skip", "true|false", "Skip this rule during scanning"),
        ("pattern", "regex", "Regex pattern for matching valid code"),
        ("anti_pattern", "regex", "Regex pattern that indicates violations"),
        ("applies_to", "list", "Code elements this rule applies to"),
        ("file_pattern", "glob", "Glob pattern for files to scan"),
        ("custom_validator", "string", "Name of custom validation function"),
        ("prebuilt", "string", "Reference to prebuilt policy template"),
        ("message", "string", "Custom message on violation"),
        ("fix_hint", "string", "Suggestion for fixing the violation"),
        ("examples.good", "string", "Example of correct code"),
        ("examples.bad", "string", "Example of incorrect code"),
        ("tags", "list", "Tags for categorization"),
        ("parameters", "object", "Additional parameters for validators"),
    ]

    pdf.set_font('Helvetica', 'B', 8)
    pdf.set_fill_color(240, 240, 240)
    pdf.cell(40, 7, 'Option', border=1, fill=True)
    pdf.cell(45, 7, 'Type', border=1, fill=True)
    pdf.cell(105, 7, 'Description', border=1, fill=True, new_x=XPos.LMARGIN, new_y=YPos.NEXT)

    pdf.set_font('Helvetica', '', 8)
    for opt, typ, desc in options:
        pdf.cell(40, 6, opt, border=1)
        pdf.cell(45, 6, typ, border=1)
        pdf.cell(105, 6, desc, border=1, new_x=XPos.LMARGIN, new_y=YPos.NEXT)

    # Prebuilt Policies
    pdf.add_page()
    pdf.set_font('Helvetica', 'B', 14)
    pdf.set_text_color(0, 51, 102)
    pdf.cell(0, 10, 'Prebuilt Policy Templates', new_x=XPos.LMARGIN, new_y=YPos.NEXT)
    pdf.ln(2)

    prebuilts = [
        ("dotnet_naming", "Standard .NET naming conventions",
         "CS-NAME-001 through CS-NAME-007"),
        ("security_essentials", "Essential security rules for financial apps",
         "CS-SEC-001-005, CS-CFG-001-002, API-AUTH-001-002, API-SAN-001"),
        ("async_best_practices", "Async/await best practices",
         "CS-ASYNC-001 through CS-ASYNC-003"),
        ("api_security", "API security standards",
         "API-CORS-001, API-VAL-001, API-ENC-001, API-AUTH-001-002, API-RATE-001"),
    ]

    pdf.set_font('Helvetica', 'B', 8)
    pdf.set_fill_color(240, 240, 240)
    pdf.cell(40, 7, 'Template', border=1, fill=True)
    pdf.cell(60, 7, 'Description', border=1, fill=True)
    pdf.cell(90, 7, 'Includes', border=1, fill=True, new_x=XPos.LMARGIN, new_y=YPos.NEXT)

    pdf.set_font('Helvetica', '', 8)
    for name, desc, includes in prebuilts:
        pdf.cell(40, 6, name, border=1)
        pdf.cell(60, 6, desc, border=1)
        pdf.cell(90, 6, includes, border=1, new_x=XPos.LMARGIN, new_y=YPos.NEXT)

    # Custom Validators
    pdf.ln(10)
    pdf.set_font('Helvetica', 'B', 14)
    pdf.set_text_color(0, 51, 102)
    pdf.cell(0, 10, 'Custom Validators', new_x=XPos.LMARGIN, new_y=YPos.NEXT)
    pdf.ln(2)

    validators = [
        ("ValidatePasswordComplexity", "Validates password meets complexity requirements"),
        ("ValidateAccountNumber", "Validates account number format"),
        ("ScanForSecrets", "Scans for potential secrets in code"),
        ("DetectSqlInjection", "Detects potential SQL injection vulnerabilities"),
        ("DetectSensitiveLogging", "Detects sensitive data in log statements"),
        ("ValidateSingleClassPerFile", "Validates one class per file"),
        ("ValidateMethodLength", "Validates method length constraints"),
        ("ValidateCyclomaticComplexity", "Validates cyclomatic complexity"),
    ]

    pdf.set_font('Helvetica', 'B', 8)
    pdf.set_fill_color(240, 240, 240)
    pdf.cell(60, 7, 'Validator', border=1, fill=True)
    pdf.cell(130, 7, 'Description', border=1, fill=True, new_x=XPos.LMARGIN, new_y=YPos.NEXT)

    pdf.set_font('Helvetica', '', 8)
    for name, desc in validators:
        pdf.cell(60, 6, name, border=1)
        pdf.cell(130, 6, desc, border=1, new_x=XPos.LMARGIN, new_y=YPos.NEXT)

    # All Policies Summary Page
    pdf.add_page()
    pdf.set_font('Helvetica', 'B', 14)
    pdf.set_text_color(0, 51, 102)
    pdf.cell(0, 10, 'Complete Policy ID List', new_x=XPos.LMARGIN, new_y=YPos.NEXT)
    pdf.ln(2)

    pdf.set_font('Courier', '', 7)
    pdf.set_text_color(0)

    all_rules = []
    for cat_id, cat_name, rules in CATEGORIES:
        for rule in rules:
            all_rules.append((rule[0], rule[1], rule[3]))

    # 3 columns
    col_width = 63
    items_per_col = (len(all_rules) + 2) // 3

    for i, (rule_id, name, severity) in enumerate(all_rules):
        col = i // items_per_col
        row = i % items_per_col
        if col < 3:
            x = 10 + col * col_width
            y = 40 + row * 5
            pdf.set_xy(x, y)
            pdf.cell(col_width, 5, f"{rule_id}: {name[:20]}")

    output_path = Path(__file__).parent / "RuleKeeper_Quick_Reference.pdf"
    pdf.output(str(output_path))
    print(f"Quick Reference PDF generated: {output_path}")
    return output_path


# =============================================================================
# PDF 2: DETAILED POLICY GUIDE
# =============================================================================
class DetailedGuidePDF(FPDF):
    def __init__(self):
        super().__init__()
        self.set_auto_page_break(auto=True, margin=15)

    def header(self):
        if self.page_no() > 1:
            self.set_font('Helvetica', 'B', 9)
            self.set_text_color(100, 100, 100)
            self.cell(0, 8, 'RuleKeeper - Detailed Policy Guide', new_x=XPos.LMARGIN, new_y=YPos.NEXT, align='C')
            self.ln(2)

    def footer(self):
        self.set_y(-15)
        self.set_font('Helvetica', 'I', 8)
        self.set_text_color(128)
        self.cell(0, 10, f'Page {self.page_no()}/{{nb}}', align='C')

    def chapter_title(self, title):
        self.set_font('Helvetica', 'B', 16)
        self.set_text_color(0, 51, 102)
        self.cell(0, 10, title, new_x=XPos.LMARGIN, new_y=YPos.NEXT)
        self.ln(4)

    def section_title(self, title):
        self.set_font('Helvetica', 'B', 12)
        self.set_text_color(51, 51, 51)
        self.cell(0, 8, title, new_x=XPos.LMARGIN, new_y=YPos.NEXT)
        self.ln(2)

    def body_text(self, text):
        self.set_font('Helvetica', '', 9)
        self.set_text_color(0)
        self.multi_cell(0, 5, text)
        self.ln(2)

    def code_block(self, code, label=""):
        if label:
            self.set_font('Helvetica', 'B', 8)
            if "Good" in label:
                self.set_text_color(0, 128, 0)
            else:
                self.set_text_color(200, 0, 0)
            self.cell(0, 4, label, new_x=XPos.LMARGIN, new_y=YPos.NEXT)
        self.set_fill_color(245, 245, 245)
        self.set_font('Courier', '', 8)
        self.set_text_color(0)
        self.multi_cell(0, 4, code, fill=True)
        self.ln(2)

    def rule_box(self, rule_id, name, description, severity, pattern=None, good=None, bad=None, fix_hint=None):
        severity_colors = {
            'critical': (220, 53, 69),
            'high': (255, 153, 0),
            'medium': (255, 193, 7),
            'low': (40, 167, 69)
        }
        color = severity_colors.get(severity, (128, 128, 128))

        # Check if we need a new page
        if self.get_y() > 220:
            self.add_page()

        y_start = self.get_y()

        # Rule header
        self.set_draw_color(*color)
        self.set_line_width(0.8)
        self.line(10, y_start, 200, y_start)

        self.set_xy(10, y_start + 2)
        self.set_font('Courier', 'B', 10)
        self.set_text_color(*color)
        self.cell(35, 5, rule_id)

        self.set_font('Helvetica', 'B', 10)
        self.set_text_color(0)
        self.cell(120, 5, name)

        self.set_font('Helvetica', 'B', 8)
        self.set_fill_color(*color)
        self.set_text_color(255)
        self.cell(25, 5, severity.upper(), fill=True, align='C')

        # Description
        self.set_xy(10, y_start + 9)
        self.set_font('Helvetica', '', 9)
        self.set_text_color(80, 80, 80)
        self.multi_cell(190, 5, description)

        # Pattern
        if pattern:
            self.set_font('Helvetica', 'B', 8)
            self.set_text_color(100, 100, 100)
            self.cell(20, 5, "Pattern:")
            self.set_font('Courier', '', 8)
            self.cell(0, 5, pattern, new_x=XPos.LMARGIN, new_y=YPos.NEXT)

        # Examples
        if good:
            self.code_block(good, "Good Example:")
        if bad:
            self.code_block(bad, "Bad Example:")

        # Fix hint
        if fix_hint:
            self.set_font('Helvetica', 'I', 8)
            self.set_text_color(0, 100, 0)
            self.multi_cell(0, 4, f"Fix: {fix_hint}")

        self.ln(5)


def generate_detailed_guide():
    pdf = DetailedGuidePDF()
    pdf.alias_nb_pages()

    # Title Page
    pdf.add_page()
    pdf.set_font('Helvetica', 'B', 32)
    pdf.set_text_color(0, 51, 102)
    pdf.ln(40)
    pdf.cell(0, 15, 'RuleKeeper', align='C', new_x=XPos.LMARGIN, new_y=YPos.NEXT)

    pdf.set_font('Helvetica', '', 18)
    pdf.set_text_color(100, 100, 100)
    pdf.cell(0, 10, 'Detailed Policy Guide', align='C', new_x=XPos.LMARGIN, new_y=YPos.NEXT)

    pdf.ln(10)
    pdf.set_font('Helvetica', 'I', 12)
    pdf.cell(0, 8, 'Parallex Bank IT Coding Standards', align='C', new_x=XPos.LMARGIN, new_y=YPos.NEXT)

    pdf.ln(20)
    pdf.set_font('Helvetica', '', 10)
    pdf.set_text_color(0)
    pdf.multi_cell(0, 6,
        "This document provides comprehensive documentation for all RuleKeeper coding standard policies. "
        "Each policy includes detailed descriptions, regex patterns, good and bad examples, and fix hints.", align='C')

    pdf.ln(10)
    pdf.set_font('Helvetica', '', 9)
    pdf.set_text_color(128)
    pdf.cell(0, 5, 'Version 1.0.0', align='C', new_x=XPos.LMARGIN, new_y=YPos.NEXT)

    # Table of Contents
    pdf.add_page()
    pdf.chapter_title('Table of Contents')
    pdf.ln(5)

    pdf.set_font('Helvetica', '', 10)
    page_num = 3
    for cat_id, cat_name, rules in CATEGORIES:
        pdf.set_text_color(0)
        pdf.cell(150, 6, cat_name)
        pdf.set_text_color(100)
        pdf.cell(0, 6, str(page_num), new_x=XPos.LMARGIN, new_y=YPos.NEXT)
        page_num += 1

    # Introduction
    pdf.add_page()
    pdf.chapter_title('Introduction')

    pdf.body_text(
        "RuleKeeper is a Policy-as-Code tool designed to scan source code and validate compliance with "
        "organizational coding standards defined in YAML configuration files."
    )

    pdf.section_title('YAML Configuration Options')
    pdf.body_text(
        "Each rule in the YAML configuration supports the following options:\n\n"
        "- enabled: true/false - Activate or deactivate the rule\n"
        "- skip: true/false - Skip this rule during scanning\n"
        "- pattern: Regex pattern that valid code should match\n"
        "- anti_pattern: Regex pattern that indicates a violation\n"
        "- custom_validator: Reference to a custom validation function\n"
        "- prebuilt: Reference to a prebuilt policy template\n"
        "- parameters: Additional configuration parameters"
    )

    pdf.section_title('Severity Levels')

    severities = [
        ("CRITICAL", (220, 53, 69), "Must be fixed immediately - security or compliance risk. Blocks deployment."),
        ("HIGH", (255, 153, 0), "Should be fixed before deployment. Requires justification to override."),
        ("MEDIUM", (255, 193, 7), "Should be addressed in the current sprint. Warning only."),
        ("LOW", (40, 167, 69), "Best practice recommendation. Informational."),
    ]

    for name, color, desc in severities:
        pdf.set_fill_color(*color)
        pdf.set_text_color(255)
        pdf.set_font('Helvetica', 'B', 10)
        pdf.cell(25, 7, name, fill=True)
        pdf.set_text_color(0)
        pdf.set_font('Helvetica', '', 9)
        pdf.cell(0, 7, f"  {desc}", new_x=XPos.LMARGIN, new_y=YPos.NEXT)
        pdf.ln(1)

    # Policy Categories
    for cat_id, cat_name, rules in CATEGORIES:
        pdf.add_page()
        pdf.chapter_title(cat_name)

        for rule_id, name, desc, severity in rules:
            details = DETAILED_RULES.get(rule_id, {})
            pdf.rule_box(
                rule_id=rule_id,
                name=name,
                description=desc,
                severity=severity,
                pattern=details.get('pattern'),
                good=details.get('good'),
                bad=details.get('bad'),
                fix_hint=details.get('fix_hint')
            )

    # Appendix: YAML Example
    pdf.add_page()
    pdf.chapter_title('Appendix: YAML Configuration Example')

    pdf.code_block("""# Example rule configuration
coding_standards:
  naming_conventions:
    - id: CS-NAME-001
      name: "Class/Interface Naming"
      description: "Classes must use PascalCase"
      severity: high
      enabled: true
      skip: false
      pattern: "^[A-Z][a-zA-Z0-9]*$"
      anti_pattern: "^[a-z]|_"
      applies_to:
        - classes
        - interfaces
      file_pattern: "**/*.cs"
      custom_validator: null
      message: "Class name must use PascalCase"
      fix_hint: "Rename with uppercase first letter"
      examples:
        good: "AccountService"
        bad: "accountservice"
      tags:
        - naming
        - convention""")

    pdf.ln(5)
    pdf.section_title('Disabling a Rule')
    pdf.code_block("""    - id: CS-NAME-001
      name: "Class/Interface Naming"
      enabled: false  # Disable this rule
      skip: true      # Also skip during scanning""")

    pdf.section_title('Using Custom Validator')
    pdf.code_block("""    - id: CS-METHOD-002
      name: "Method Length"
      custom_validator: "Validators.ValidateMethodLength"
      parameters:
        max_lines: 50  # Custom parameter""")

    output_path = Path(__file__).parent / "RuleKeeper_Detailed_Guide.pdf"
    pdf.output(str(output_path))
    print(f"Detailed Guide PDF generated: {output_path}")
    return output_path


# =============================================================================
# MAIN
# =============================================================================
if __name__ == "__main__":
    print("Generating RuleKeeper PDFs...")
    print()
    generate_quick_reference()
    generate_detailed_guide()
    print()
    print("Done! Both PDFs have been generated.")
