Feature: Request Statement

  Scenario: Citizen requests their tax statement
    Given an employee named "John Doe" with CPR "010101-1234"
    And the employee's annual salary is reported as 100000
    When the salary report is generated
    Then the report should contain "John Doe" and a gross income of 100000