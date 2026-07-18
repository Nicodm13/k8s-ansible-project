Feature: Register Citizen

  Scenario: Registering a new citizen via the API
    Given a citizen with CPR "010101-1234" named "John" "Doe" living at "Main Street 1", "Copenhagen", "1000" with bank account "1234567890"
    When the citizen registration is submitted
    Then the response status code should be 200
    And the response body should contain CPR "010101-1234"

