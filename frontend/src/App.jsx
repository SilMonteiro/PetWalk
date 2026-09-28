import { BrowserRouter, Routes, Route } from 'react-router-dom';
import LoginScreen from './components/LoginScreen';
import HomeScreen from './components/HomeScreen';
import { PetRegistrationScreen } from './components/PetRegistrationScreen';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<LoginScreen />} />
        <Route path="/home" element={<HomeScreen />} />
        <Route path="/registro-pet" element={<PetRegistrationScreen />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;