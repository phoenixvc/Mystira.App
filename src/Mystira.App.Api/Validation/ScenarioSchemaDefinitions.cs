namespace Mystira.App.Admin.Api.Validation;

public static class ScenarioSchemaDefinitions
{
    public const string StorySchema = """
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "Mystira Story Schema",
  "description": "Validation schema for Mystira story YAML/JSON payloads",
  "type": "object",
  "additionalProperties": false,
  "required": [
    "title",
    "description",
    "tags",
    "difficulty",
    "session_length",
    "age_group",
    "minimum_age",
    "core_axes",
    "archetypes",
    "characters",
    "scenes"
  ],
  "properties": {
    "title": { "type": "string", "minLength": 1, "maxLength": 200, "description": "The title of the story" },
    "description": { "type": "string", "minLength": 1, "maxLength": 1000, "description": "Brief description of the story" },
    "tags": {
      "type": "array",
      "items": { "type": "string" },
      "minItems": 1,
      "description": "Array of story tags/categories"
    },
    "difficulty": { "type": "string", "enum": ["Easy", "Medium", "Hard"], "description": "Story difficulty level" },
    "session_length": { "type": "string", "enum": ["Short", "Medium", "Long"], "description": "Expected session duration" },
    "age_group": { "type": "string", "enum": ["1-2", "3-5", "6-9", "10-12", "13-18", "19-150"], "description": "Target age group" },
    "minimum_age": { "type": "integer", "enum": [1, 3, 6, 10, 13], "description": "Minimum recommended age (controls allowed age_group bands)" },
    "core_axes": { "type": "array", "items": { "type": "string" }, "minItems": 1, "description": "Core story themes/axes" },
    "archetypes": { "type": "array", "items": { "type": "string" }, "minItems": 1, "description": "Character archetypes present in the story" },
    "image": { "type": "string", "description": "Image ID for the cover picture of the scenario" },

    "characters": {
      "type": "array",
      "minItems": 1,
      "description": "Array of story characters",
      "items": {
        "type": "object",
        "additionalProperties": false,
        "required": ["id", "name", "metadata"],
        "properties": {
          "id": {
            "type": "string",
            "pattern": "^[a-z0-9_]+$",
            "minLength": 1,
            "description": "Character id (lowercase snake_case)"
          },
          "name": { "type": "string", "minLength": 1, "description": "Character name" },
          "image": { "type": "string", "minLength": 1, "description": "Image id" },
          "audio": { "type": "string", "minLength": 1, "description": "Audio id" },
          "metadata": {
            "type": "object",
            "additionalProperties": false,
            "required": ["role", "archetype", "species", "age", "traits", "backstory"],
            "properties": {
              "role": { "type": "array", "items": { "type": "string" }, "minItems": 1, "description": "Character's role in the story" },
              "archetype": { "type": "array", "items": { "type": "string" }, "minItems": 0, "description": "Character archetype" },
              "species": { "type": "string", "minLength": 1, "description": "Character species" },
              "age": { "type": "integer", "minimum": 1, "description": "Character age" },
              "traits": { "type": "array", "items": { "type": "string" }, "minItems": 1, "description": "Character traits" },
              "backstory": { "type": "string", "minLength": 1, "description": "Character backstory" }
            }
          }
        }
      }
    },

    "scenes": {
      "type": "array",
      "minItems": 1,
      "description": "Array of story scenes",
      "items": {
        "type": "object",
        "additionalProperties": false,
        "required": ["id", "title", "type", "description"],
        "properties": {
          "id": {
            "type": "string",
            "pattern": "^[A-Za-z0-9_]+$",
            "minLength": 1,
            "description": "Unique scene identifier (snake_case; letters/numbers/_ allowed)"
          },
          "title": { "type": "string", "minLength": 1, "description": "Scene title" },
          "type": { "type": "string", "enum": ["narrative", "choice", "roll", "special"], "description": "Scene type" },
          "description": { "type": "string", "minLength": 1, "description": "Scene description" },
          "next_scene": {
            "anyOf": [
              {
                "type": "string",
                "minLength": 1,
                "pattern": "^[A-Za-z0-9_]+$"
              },
              {
                "type": "null"
              }
            ],
            "description": "Next scene id (for linear flow). Must be null for final/special scenes."
          },
          "difficulty": { "type": "number", "minimum": 1, "maximum": 20, "description": "Scene difficulty (required for roll type scenes)" },
          "media": {
            "type": "object",
            "description": "Optional media attached to this scene; at least one of image/audio/video should be present",
            "additionalProperties": false,
            "minProperties": 1,
            "properties": {
              "image": { "type": "string", "description": "Image id or path" },
              "audio": { "type": "string", "description": "Audio id or path" },
              "video": { "type": "string", "description": "Video id or path" }
            }
          },

          "branches": {
            "type": "array",
            "description": "Scene branches (required for choice and roll type scenes)",
            "items": {
              "type": "object",
              "additionalProperties": false,
              "required": ["choice", "next_scene"],
              "properties": {
                "choice": { "type": "string", "minLength": 1, "description": "Branch choice description" },
                "next_scene": {
                  "type": "string",
                  "minLength": 1,
                  "pattern": "^[A-Za-z0-9_]+$",
                  "description": "The ID of the next scene to which this choice should navigate"
                },
                "echo_log": {
                  "type": "object",
                  "additionalProperties": false,
                  "description": "Echo recorded by taking this branch",
                  "required": ["echo_type", "description", "strength"],
                  "properties": {
                    "echo_type": { "type": "string", "minLength": 1, "description": "The type of echo" },
                    "description": { "type": "string", "minLength": 1, "description": "The description of the echo" },
                    "strength": { "type": "number", "minimum": -1.0, "maximum": 1.0, "description": "The strength of the echo" }
                  }
                },

                "compass_change": {
                  "type": "object",
                  "additionalProperties": false,
                  "description": "Optional compass change values",
                  "required": ["axis", "delta"],
                  "properties": {
                    "axis": { "type": "string", "minLength": 1, "description": "Axis name to adjust" },
                    "delta": { "type": "number", "description": "Change applied to the axis" },
                    "developmental_link": { "type": "string", "minLength": 1, "description": "Reference to developmental framework link" }
                  }
                }
              }
            }
          },

          "echo_reveals": {
            "type": "array",
            "description": "Echo reveal references",
            "items": {
              "type": "object",
              "additionalProperties": false,
              "required": ["echo_type", "min_strength", "trigger_scene_id"],
              "properties": {
                "echo_type": { "type": "string", "minLength": 1, "description": "Type of echo" },
                "min_strength": { "type": "number", "description": "Minimum strength required" },
                "trigger_scene_id": {
                  "type": "string",
                  "minLength": 1,
                  "pattern": "^[A-Za-z0-9_]+$",
                  "description": "Scene ID that triggers the reveal"
                },
                "max_age_scenes": { "type": "integer", "minimum": 0, "description": "Optional max age in scenes for the echo" },
                "reveal_mechanic": { "type": "string", "minLength": 1, "description": "Mechanic used to reveal" },
                "required": { "type": "boolean", "description": "If true, this reveal must trigger when conditions are met" }
              }
            }
          }
        },
        "allOf": [
          { "if": { "properties": { "type": { "const": "roll" } } }, "then": { "required": ["difficulty", "branches"] } },
          { "if": { "properties": { "type": { "const": "choice" } } }, "then": { "required": ["branches"] } },
          { "if": { "properties": { "type": { "const": "narrative" } } }, "then": { "required": ["next_scene"] } },
          { "if": { "properties": { "type": { "const": "special" } } }, "then": { "properties": { "next_scene": { "type": "null" } } } }
        ]
      }
    }
  },
  "allOf": [
    {
      "if": { "properties": { "minimum_age": { "const": 1 } } },
      "then": { "properties": { "age_group": { "enum": ["1-2", "3-5", "6-9", "10-12", "13-18", "19-150"] } } }
  },
  {
      "if": { "properties": { "minimum_age": { "const": 3 } } },
      "then": { "properties": { "age_group": { "enum": ["3-5", "6-9", "10-12", "13-18", "19-150"] } } }
  },
  {
      "if": { "properties": { "minimum_age": { "const": 6 } } },
      "then": { "properties": { "age_group": { "enum": ["6-9", "10-12", "13-18", "19-150"] } } }
  },
  {
      "if": { "properties": { "minimum_age": { "const": 10 } } },
      "then": { "properties": { "age_group": { "enum": ["10-12", "13-18", "19-150"] } } }
  },
  {
      "if": { "properties": { "minimum_age": { "const": 13 } } },
      "then": { "properties": { "age_group": { "enum": ["13-18", "19-150"] } } }
  }
  ]
}
""";
}
