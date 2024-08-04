import React from 'react';
import { Box,  Flex } from '@chakra-ui/react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import SideNav from './components/SideNav';
import HomePage from './pages/HomePage';
import AffairPage from './pages/AffairPage';
import './styles/rtl.css'; 
import SectorialJointAgreementForm from './components/sectorialJointAgreement/SectorialJointAgreementForm';
import SectorialJointAgreementPage from './pages/SectorialJointAgreementPage';
import SalaryTableForm from './components/salaryTables/SalaryTableForm';
import SalaryTablesList from './components/salaryTables/SalaryTablesList';
import StatusBar from './components/StatusBar';


const App: React.FC = () => {
  return (
    <Router>
      <Flex direction="row">
        <SideNav />
        <Box p={4} flex="1">
          <Routes>
            <Route path='/agreements' element={<SectorialJointAgreementPage />} />
            <Route path='/agreement-form' element={<SectorialJointAgreementForm />} />
            <Route path='/agreement-form/:sectorialJointAgreementId' element={<SectorialJointAgreementForm />} />
            <Route path="/salary-table-form" element={<SalaryTableForm  />} />
            <Route path="/salary-table-form/:salaryTableId" element={<SalaryTableForm />} />
            <Route path="/salary-tables" element={<SalaryTablesList />} />
            <Route path="/salary-tables/:sectorialJointAgreementId" element={<SalaryTablesList />} />
            <Route path="/affairs" element={<AffairPage />} />
            <Route path="/" element={<HomePage />} />
            
          </Routes>
          <StatusBar />
        </Box>
      </Flex>
    </Router>
  );
};

export default App;
