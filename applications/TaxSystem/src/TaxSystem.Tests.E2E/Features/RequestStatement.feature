Feature: Request Statement

  Scenario: Citizen requests their tax statement
    Given a company with CVR "87654321"
    And an employee named "Jane Doe" with CPR "101019876"
    And the employee's annual salary is reported as 100000 and paid tax as 0
    When the statement is generated
    Then the report should contain "Jane Doe" and a gross income of 100000
    And a bank transfer of 37000 should be scheduled

  Scenario: Citizen requests their tax statement after company reports salary
    Given a company with CVR "11223344"
    And an employee named "John Doe" with CPR "111011234"
    And the employee's annual salary is reported as 100000 and paid tax as 30000
    When the statement is generated
    Then the statement should contain "John Doe" and a gross income of 100000

  Scenario: Citizen reports their deductibles, then requests their tax statement
    Given an employee named "Jane Roe" with CPR "222012345"
    And the employee reports deductibles of 20000
    Then no statement should be available for the employee

  Scenario: Citizen reports their deductibles, company reports salary, then citizen requests their tax statement
    Given a company with CVR "55667788"
    And an employee named "Alice Smith" with CPR "333013456"
    And the employee reports deductibles of 20000
    And the employee's annual salary is reported as 100000 and paid tax as 30000
    When the statement is generated
    Then the statement should contain "Alice Smith" and a gross income of 100000
    And the statement should contain a deductible of 20000
