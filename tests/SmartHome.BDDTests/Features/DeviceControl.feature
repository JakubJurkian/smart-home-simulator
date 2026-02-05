Feature: Device Control
  As a homeowner
  I want to verify that my smart devices respond to commands
  So that I can trust my smart home system

  Scenario: Turning on a light bulb
    Given I am a registered user named "John"
    And I have a room named "Living Room"
    And I have a device named "Table Lamp" of type "LightBulb" in "Living Room"
    When I send a request to turn on "Table Lamp"
    Then The device "Table Lamp" should be ON