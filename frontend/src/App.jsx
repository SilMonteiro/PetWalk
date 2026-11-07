import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import RotaProtegida from './components/RotaProtegida';
import LoginPage from './pages/LoginPage';
import CadastroPage from './pages/CadastroPage';
import HomePage from './pages/HomePage';
import PetsPage from './pages/PetsPage';
import PetFormPage from './pages/PetFormPage';

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/" element={<LoginPage />} />
          <Route path="/cadastro" element={<CadastroPage />} />
          <Route
            path="/home"
            element={
              <RotaProtegida>
                <HomePage />
              </RotaProtegida>
            }
          />
          <Route
            path="/pets"
            element={
              <RotaProtegida>
                <PetsPage />
              </RotaProtegida>
            }
          />
          <Route
            path="/pets/novo"
            element={
              <RotaProtegida>
                <PetFormPage />
              </RotaProtegida>
            }
          />
          <Route
            path="/pets/:id/editar"
            element={
              <RotaProtegida>
                <PetFormPage />
              </RotaProtegida>
            }
          />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
