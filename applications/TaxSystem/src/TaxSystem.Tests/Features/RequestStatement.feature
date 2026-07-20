Feature: Request Statement

  Scenario: Citizen requests their tax statement
    Given an employee named "John Doe" with CPR "010101-1234"
    And the employee's annual salary is reported as 100000 with a paid tax of 30000
    When the salary report is generated
    Then the report should contain "John Doe" and a gross income of 100000
    
   Scenario: Citizen reports their deductibles, then requests their tax statement
    Given an employee named "Jane Doe" with CPR "020202-5678"
    And the employee's annual salary is reported as 100000 with a paid tax of 30000
    And the employee reports deductibles of 20000
    When the salary report is generated
    Then No report should be generated
    And A message should be sent containing "Tax info has not been reported"
    
    Scenario: Citizen reports their deductibles, company reports salary, then citizen requests their tax statement
    Given an employee named "Alice Smith" with CPR "030303-9876"
    And the employee reports deductibles of 20000
    And the employee's annual salary is reported as 100000 with a paid tax of 30000
    When the salary report is generated
    Then the report should contain "Alice Smith" and a gross income of 100000
    And the report should contain "Alice Smith" and a deductible of 20000
    