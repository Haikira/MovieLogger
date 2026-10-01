import { Compass } from 'lucide-react';
import { PageHeader } from '../components/layout/PageHeader';
import { ButtonLink } from '../components/ui/Button';
import { Card } from '../components/ui/Controls';
import { EmptyState } from '../components/ui/Feedback';

export function NotFoundPage() {
  return (
    <>
      <PageHeader title="Page not found" />
      <Card>
        <EmptyState
          icon={<Compass size={22} />}
          title="There's nothing here"
          description="The page you were looking for doesn't exist."
          actions={<ButtonLink to="/dashboard">Go to Dashboard</ButtonLink>}
        />
      </Card>
    </>
  );
}
