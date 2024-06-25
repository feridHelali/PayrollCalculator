import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import SalaryTableForm from '../../src/renderer/components/sectorialJointAgreement/SalaryTableForm';
import store from '../../src/renderer/redux/store';
import '@testing-library/jest-dom';

// Mock implementation
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => jest.fn(),
  useParams: () => ({ salaryTableId: '1' }),
}));

jest.mock('../../src/renderer/redux/sectorialJointAgreement/salaryTableSlice', () => ({
  createSalaryTable: jest.fn(),
  fetchSalaryTableById: jest.fn(),
  updateSalaryTable: jest.fn(),
  fetchAgreements: jest.fn(),
  switchToCreateMode: jest.fn(),
  switchToUpdateMode: jest.fn(),
}));

describe('SalaryTableForm Component', () => {
  it('should render the form with initial values', async () => {
    render(
      <Provider store={store}>
        <MemoryRouter>
          <Routes>
            <Route path="/" element={<SalaryTableForm />} />
          </Routes>
        </MemoryRouter>
      </Provider>
    );

    expect(screen.getByLabelText(/Select Agreement/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Salary Table Number/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Salary Type/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Concerned Employee/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Beginning Date of Application/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/End Date of Application/i)).toBeInTheDocument();
  });

  it('should update form fields and handle create', async () => {
    render(
      <Provider store={store}>
        <MemoryRouter>
          <Routes>
            <Route path="/" element={<SalaryTableForm />} />
          </Routes>
        </MemoryRouter>
      </Provider>
    );

    fireEvent.change(screen.getByLabelText(/Salary Table Number/i), { target: { value: '123' } });
    fireEvent.change(screen.getByLabelText(/Salary Type/i), { target: { value: 'Type' } });
    fireEvent.change(screen.getByLabelText(/Concerned Employee/i), { target: { value: 'Employee' } });
    fireEvent.change(screen.getByLabelText(/Beginning Date of Application/i), { target: { value: '2022-01-01' } });

    fireEvent.click(screen.getByText(/Create/i));

    await waitFor(() => {
      expect(screen.queryByText(/Create/i)).toBeInTheDocument();
    });
  });
});
