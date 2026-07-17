Feature: Report Salary

  Scenario: Company reporting salary of a citizen
    Given a company with CVR "12345678"
    And an employee named "John Doe" with CPR "010101-1234"
    And the employee's annual salary is reported as 100000
    When the statement is generated
    Then the statement should contain "John Doe" and a gross income of 100000

