import React from 'react';
import { Box, Heading, Text, Flex } from '@chakra-ui/react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import SideNav from './components/SideNav';
import HomePage from './pages/HomePage';
import PayrollPage from './pages/PayrollPage';

const App: React.FC = () => {
  return (
    <Router>
      <Flex direction="row">
        <SideNav />
        <Box p={4} flex="1">
          <Routes>
            <Route path="/payroll" element={<PayrollPage />} />
            <Route path="/" element={<HomePage />} />
          </Routes>
        </Box>
      </Flex>
    </Router>
  );
};

export default App;
