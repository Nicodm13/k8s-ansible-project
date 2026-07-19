Feature: Request Statement

  Scenario: Citizen requests their tax statement
    Given a company with CVR "87654321"
    And an employee named "Jane Doe" with CPR "101019876"
    And the employee's annual salary is reported as 100000
    When the statement is generated
    Then the report should contain "Jane Doe" and a gross income of 100000

