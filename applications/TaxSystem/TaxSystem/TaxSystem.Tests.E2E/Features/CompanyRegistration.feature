Feature: Company Registration

  Scenario: Government registers a company and the company can be looked up
    Given a company named "Acme E2E Corp" with CVR "87654321"
    When the company is registered through the client API
    Then the company lookup should return "Acme E2E Corp" with CVR "87654321"
