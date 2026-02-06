import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import Layout from './components/Layout';
import StudyList from './components/StudyList';
import AdvancedViewer from './components/AdvancedViewer';
import './styles/index.css';

// Dark theme optimized for medical imaging
const darkTheme = createTheme({
  palette: {
    mode: 'dark',
    primary: {
      main: '#4fc3f7',
    },
    secondary: {
      main: '#ff8a65',
    },
    background: {
      default: '#0a0a0a',
      paper: '#1a1a1a',
    },
    text: {
      primary: '#ffffff',
      secondary: '#b0b0b0',
    },
  },
  typography: {
    fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: 'none',
        },
      },
    },
  },
});

function App() {
  return (
    <ThemeProvider theme={darkTheme}>
      <CssBaseline />
      <BrowserRouter>
        <Layout>
          <Routes>
            <Route path="/" element={<StudyList />} />
            <Route path="/viewer" element={<AdvancedViewer />} />
            <Route path="/viewer/:studyId" element={<AdvancedViewer />} />
            <Route path="/viewer/:studyId/:seriesId" element={<AdvancedViewer />} />
            <Route path="/view/:encryptedStudyUid" element={<AdvancedViewer />} />
          </Routes>
        </Layout>
      </BrowserRouter>
    </ThemeProvider>
  );
}

export default App;
