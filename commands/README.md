# Spec-Kit Commands

This directory contains command templates used by the spec-kit MCP server.

## Available Commands

### speckit.specify
Defines what needs to be built. Creates a specification document that describes the requirements, user stories, and acceptance criteria for a feature.

### speckit.plan
Creates a technical plan based on the specification. Describes the architecture, components, and implementation approach.

### speckit.tasks
Breaks down the plan into actionable tasks. Each task should be small enough to be completed in a single development session.

### speckit.implement
Executes the implementation based on the tasks. Writes the actual code following the plan.

### speckit.constitution
Establishes or reviews the project constitution - the foundational principles that guide all development decisions.

## Workflow

1. **Specify** → Define what to build
2. **Plan** → Design how to build it
3. **Tasks** → Break into steps
4. **Implement** → Write the code
5. **Review** → Ensure quality

## Templates

Each command file defines the template that the spec-kit tool uses when called. Templates are automatically loaded from this directory.
