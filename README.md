PROG6212 POE Part 2: Contract Monthly Claim System (CMCS) Prototype
This repository contains the final source code and updated documentation for the Part 2 submission of the Programming 2B (PROG6212) Portfolio of Evidence (POE).

Documentation
Architecture and Data Persistence Strategy
The application is built on the ASP.NET Core MVC framework, chosen for its separation of concerns (Models, Views, Controllers), promoting maintainable and scalable web-based code (Troelsen & Japikse, 2022). This cross-platform approach ensures remote accessibility for lecturers and managers.

Data Persistence Shift (Part 2 Update)
The initial plan to use Entity Framework Core (EF Core) and a relational database has been removed to comply with Part 2 requirements.

Current Strategy: Claims are managed by a dedicated Data Service class that holds claims in in-memory collections. This facilitates quick access and manipulation.

Security & File Handling: Supporting document files are securely stored by applying a basic encryption mechanism upon upload and decryption upon download, adhering to the security requirement.


Constraints and Assumptions
Constraint: No Identities/Roles – Access is simulated via hardcoded IDs to differentiate the Lecturer, Programme Coordinator, and Academic Manager views. No login/registration is implemented.

Constraint: Validation – Business rules are enforced via Data Annotation attributes (e.g., enforcing an hour range between 1 and 160).

Assumption: The hourly rate is pre-established and read-only on the submission form.

Design: The GUI is designed to be user-friendly and intuitive using Bootstrap for responsive layouts, minimizing potential submission errors (Japikse & Troelsen, 2022).



Project Plan (Agile Approach)
The project development followed an agile, iterative approach over a 2-week period, ensuring systematic development guided by dependencies, milestones, and risk mitigation strategies (Troelsen & Japikse, 2022). The plan approximated a total effort of about 38 hours.

Development was guided by the agile principles of iterative development (Troelsen & Japikse, 2022).

Version Control: Regular commits and pushes to GitHub were maintained, ensuring a minimum of 10 commits with descriptive messages.

Milestones: Week 1 focused on initial research, schema design, and basic setup. Week 2 focused on core Part 2 feature implementation, testing, and final documentation.



GUI/UI Implemented Features
The UI is built using ASP.NET Core MVC views, emphasizing responsive design via Bootstrap for cross-device compatibility (Troelsen & Japikse, 2022).

Lecturer View
Claim Submission (Submit.cshtml): Features a form for Hours Worked (with validation), Hourly Rate (read-only), Module, and Notes.

Document Upload: Supports multi-file upload with validation enforcing a Max 5MB size limit and restricted file types: .pdf, .docx, and .xlsx. The successful upload shows the file name, and the controller handles secure encryption (Japikse & Troelsen, 2022).

Claim Tracking (ViewClaims.cshtml): Claims are listed with a visual status tracker: a progress bar showing 33% (Pending), 66% (Under Review), or 100% (Approved/Rejected).


Administrator Views (X2 Separate Views)
The approval workflow is separated into two distinct pages:

Programme Coordinator View (CoordinatorApprove.cshtml): Displays Pending claims only. Actions: Verify (moves status to Under Review) and Reject.

Academic Manager View (ManagerApprove.cshtml): Displays Under Review claims only. Actions: Approve (final status) and Reject.

Both views display necessary claim information and include links to view/download the supporting documents, triggering the system's decryption logic.




References
Japiklse, P. & Troelsen, A., 2022. In: Pro C# 10 with .NET 6: Foundational Principles and Practices in Programming. New York: Apress, p. 921.

Japikse, P. & Troelsen, A., 2022. In: Pro C# 10 with .NET 6: Foundational Principles and Practices in Programming. New York: Apress, p. 847.

Japikse, P. & Troelsen, A., 2022. In: Pro C# 10 with .NET 6: Foundational Principles and Practices in Programming. New York: Apress, p. 1520.

Japikse, P. & Troelsen, A., 2022. In: Pro C# 10 with .NET 6: Foundational Principles and Practices in Programming. New York: Apress, p. 1007.

Japikse, P. & Troelsen, A., 2022. In: Pro C# 10 with .NET 6: Foundational Principles and Practices in Programming. New York: Apress, p. 1015.

Japkise, P. & Troelsen, A., 2022. In: Pro C# 10 with .NET 6: Foundational Principles and Practices in Programming. New York: Apress, p. 911.

Troelsen, A. & Japikse, P., 2022. In: Pro C# 10 with .NET 6: Foundational Principles and Practices in Programming. New York: Apress, p. 1359.

Troelsen, A. & Japikse, P., 2022. In: Pro C# 10 with .NET 6: Foundational Principles and Practices in Programming. New York: Apress, p. 1364.

Troelsen, A. & Japikse, P., 2022. In: Pro C# 10 with .NET 6: Foundational Principles and Practices in Programming. New York: Apress, p. 959.

Troelsen, A. & Japikse, P., 2022. In: Pro C# 10 with .NET 6: Foundational Principles and Practices in Programming. New York: Apress, p. 1039.

Troelsen, A. & Japikse, P., 2022. Pro C# 10 with .NET 6: Foundational Principles and Practices in Programming. 11 ed. New York: Apress.
