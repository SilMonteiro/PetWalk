import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import RotaProtegida from './components/RotaProtegida';
import LoginPage from './pages/LoginPage';
import HomePage from './pages/HomePage';
import { PetFormPage } from './pages/PetFormPage';

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/" element={<LoginPage />} />
          <Route
            path="/home"
            element={
              <RotaProtegida>
                <HomePage />
              </RotaProtegida>
            }
          />
          <Route path="/registro-pet" element={<PetFormPage />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
