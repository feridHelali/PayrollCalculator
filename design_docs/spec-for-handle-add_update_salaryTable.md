### Explanation:

1. **SalaryTableInput Component**: This component now correctly initializes the `salaries` state based on the `headers` (degrees and ages of work) and `categories`. It dynamically generates input fields for each combination of category, degree, and age of work. The `handleChange` function updates the `salaries` state whenever a value in an input field changes.

2. **SalaryTableForm Component**: 
   - **Initialization and State Management**: `degrees`, `categories`, and `headers` are managed as state variables. `degrees` and `workingAges` are updated when adding a new degree and age of work.
   - **Handling Inputs and Selections**: The form allows selection of agreement, input of table number, selection of salary type, input of concerned employee, and selection of beginning and end dates of application.
   - **Handling Degree and Age of Work Addition**: `handleAddDegreeAndAgeOfWork` adds a new degree and age of work to the respective state variables.
   - **Handling Category Addition**: `handleAddCategory` adds a new category to the `categories` state.
   - **Saving**: `handleSave` constructs the complete salary table object and dispatches it for creation.

### Usage:
- The `SalaryTableForm` component is used to create or update a salary table entry. It initializes necessary state variables and manages form inputs and selections.
- The `SalaryTableInput` component generates a table based on the provided headers (degrees and ages of work) and categories, allowing users to input salaries corresponding to each category, degree, and age of work combination.

This setup should fit your requirement of generating salary table headers and rows based on degrees and ages of work for inputting salaries. Adjustments can be made based on additional requirements or specific UI/UX preferences.