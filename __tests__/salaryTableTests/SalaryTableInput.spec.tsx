import { render, screen, fireEvent } from '@testing-library/react';
import SalaryTableInput from '../../src/renderer/components/sectorialJointAgreement/SalaryTableInput';
import '@testing-library/jest-dom';  // Ensure this is imported if not in a setup file
import React from 'react';

const headers = [
  { degree: 1, ageOfWork: 1 },
  { degree: 2, ageOfWork: 2 },
];

const categories = ['Category 1', 'Category 2'];

describe('SalaryTableInput Component', () => {
  it('should render the table with headers and categories', () => {
    render(<SalaryTableInput headers={headers} categories={categories} onSave={jest.fn()} />);

    const degreeTextMatcher = (content: string) =>
      content.includes('Degree 1') && content.includes('Working Age 1');

    expect(screen.getByText(degreeTextMatcher)).toBeInTheDocument();

    categories.forEach((category) => {
      expect(screen.getByText(category)).toBeInTheDocument();
    });
  });

  it('should update salaries and handle save', () => {
    const mockOnSave = jest.fn();
    render(<SalaryTableInput headers={headers} categories={categories} onSave={mockOnSave} />);

    const salaryInput = screen.getByLabelText(/Degree 1 \(Working Age 1\) - Category 1/i);
    fireEvent.change(salaryInput, { target: { value: '1000' } });
    fireEvent.click(screen.getByText(/Save/i));

    expect(mockOnSave).toHaveBeenCalledWith({ /* expected value here */ });
  });
});
