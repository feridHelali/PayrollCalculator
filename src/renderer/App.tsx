import React from 'react';
import { Box, Heading, Text, Flex } from '@chakra-ui/react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import SideNav from './components/SideNav';
import HomePage from './pages/HomePage';
import AffairPage from './pages/AffairPage';
import './styles/rtl.css'; 
import SectorialJointAgreementList from './pages/SectorialJointAgreementPage';

const App: React.FC = () => {
  console.log(window.electronAPI);

  return (
    <Router>
      <Flex direction="row">
        <SideNav />
        <Box p={4} flex="1">
          <Routes>
            <Route path='/agreements' element={<SectorialJointAgreementList />} />
            <Route path="/affairs" element={<AffairPage />} />
            <Route path="/" element={<HomePage />} />
            
          </Routes>
        </Box>
      </Flex>
    </Router>
  );
};

export default App;
