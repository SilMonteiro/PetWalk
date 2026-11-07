import { BrowserRouter, Routes, Route } from 'react-router-dom';
import LoginPage from './pages/LoginPage';
import HomePage from './pages/HomePage';
import { PetFormPage } from './pages/PetFormPage';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<LoginPage />} />
        <Route path="/home" element={<HomePage />} />
        <Route path="/registro-pet" element={<PetFormPage />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
