import { Navigate, Route, Routes } from 'react-router';
import { PublicOnly, RequireAuth } from './auth/RouteGuards';
import { AppLayout } from './components/layout/AppLayout';
import { AddMoviePage } from './pages/AddMoviePage';
import { DashboardPage } from './pages/DashboardPage';
import { LoginPage } from './pages/LoginPage';
import { LogMoviePage } from './pages/LogMoviePage';
import { MovieDetailsPage } from './pages/MovieDetailsPage';
import { MyMoviesPage } from './pages/MyMoviesPage';
import { NotFoundPage } from './pages/NotFoundPage';
import { RegisterPage } from './pages/RegisterPage';
import { SearchPage } from './pages/SearchPage';
import { SettingsPage } from './pages/SettingsPage';
import { WatchlistPage } from './pages/WatchlistPage';

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<PublicOnly><LoginPage /></PublicOnly>} />
      <Route path="/register" element={<PublicOnly><RegisterPage /></PublicOnly>} />

      <Route element={<RequireAuth><AppLayout /></RequireAuth>}>
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/my-movies" element={<MyMoviesPage />} />
        <Route path="/search" element={<SearchPage />} />
        <Route path="/movies/new" element={<AddMoviePage />} />
        <Route path="/movies/:movieId" element={<MovieDetailsPage />} />
        <Route path="/log" element={<LogMoviePage />} />
        <Route path="/logs/:watchId/edit" element={<LogMoviePage />} />
        <Route path="/watchlist" element={<WatchlistPage />} />
        <Route path="/settings" element={<SettingsPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  );
}
