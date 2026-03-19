# TourPlanner Requirements 2026

## Scope

This document defines the functional scope and completion criteria for the TourPlanner project.
It is intended to help students work with AI in a way that resembles professional software delivery.

## Functional Requirements

### Tour Management

- [ ] User can create a new tour
- [ ] User can edit an existing tour
- [ ] User can delete an existing tour
- [ ] User can view a list of all tours
- [ ] User can search and filter tours
- [ ] Selected tour details are displayed

### Tour Data

- [ ] Tour stores name
- [ ] Tour stores description
- [ ] Tour stores start location
- [ ] Tour stores destination
- [ ] Tour stores transport type
- [ ] Tour stores distance
- [ ] Tour stores estimated time
- [ ] Tour stores route information
- [ ] Tour stores image reference

### Tour Log Management

- [ ] User can create a tour log
- [ ] User can edit a tour log
- [ ] User can delete a tour log
- [ ] User can view all logs for the selected tour

### Tour Log Data

- [ ] Tour log stores date and time
- [ ] Tour log stores comment
- [ ] Tour log stores difficulty
- [ ] Tour log stores distance
- [ ] Tour log stores duration
- [ ] Tour log stores rating

### Routing and Map

- [ ] Route is calculated from selected start and destination
- [ ] Route reflects selected transport type
- [ ] Route is visualized on a map
- [ ] Tour detail view shows route-related information

### Reports and File Features

- [ ] Detailed PDF report can be generated for a single tour
- [ ] Summary PDF report can be generated for all tours
- [ ] Tour data can be exported
- [ ] Tour data can be imported

### Backend

- [ ] Backend API exists for tour operations
- [ ] Backend API exists for tour log operations
- [ ] Frontend communicates with backend successfully

## Acceptance Criteria

### UI Rules

- [ ] UI prevents invalid actions
- [ ] Add and edit actions are only available in a valid state
- [ ] Save is only possible when required fields are valid
- [ ] Conflicting UI states are prevented
- [ ] Selected tour drives dependent UI state
- [ ] Selected log drives dependent UI state

### Architecture

- [ ] UI layer is separated
- [ ] API layer is separated
- [ ] Business logic layer is separated
- [ ] Data access layer is separated
- [ ] Logging layer is separated
- [ ] Test project is separated

## Definition of Done

See [DEFINITION_OF_DONE.md](../DEFINITION_OF_DONE.md).
