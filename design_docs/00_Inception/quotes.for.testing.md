## Testing

```text
“The more your tests resemble the way your software is used, the more confidence they can give you.” - Kent C. Dodds, creator of React Testing Library"
```

### ADVANTAGES OF WRITING TESTS
Helps us to detect bugs earlier
Provides us with our own documentation on the code
Forces us to write better code when building small components with single responsibility
Ensures the code will be more consistent across the team
Gives us more confidence because it makes the application easy to maintain, allowing developers to safely refactor the code
TYPES OF TESTS
Unit testing: is used to validate the operation of the smallest code units in the system. The idea is that this type of test needs to be simple and without any dependence on external functionality - of course, there are some exceptions where the component needs to work with data from the database or some other source, in which case we use mock (let's see more about that later).
Functional testing: checks if multiple units work together.
HOW TO IDENTIFY UNIT AND FUNCTIONAL TESTING
Let's imagine a search field that, when clicked, expands. When typing something, a suggestion box is shown with data from an API; and if there are no suggestions, a feedback message is displayed.

Unit tests can be: check if the search field will expand when receiving focus; check if, when typing something, the API fetch method is called; check if there are no suggestions, a feedback message displayed…

Function tests can be: user simulation by clicking, typing, deleting...

### HOW TO THINK TESTING
There is a good practice called test-driven development (TDD) that is used to ensure that our tests will cover all the functionality that the component must have before creating the component itself. In this way, we guarantee that the code will be agnostic, that is, everything has to work regardless of how the code was implemented.

Basically the process is:

First, we need to create all the tests and they will all fail
We then provide a simple solution for all tests to pass
And finally, we refactor the solution in the best possible way
Why is it important to create the tests before the component code?

If the tests are created last, we tend to write the tests according to the code that was implemented, instead of the problem that must be solved. That is, the test can be oriented towards implementation instead of functionality, which can be a problem if the solution/implementation changes in the future.