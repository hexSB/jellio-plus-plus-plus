import { FC } from 'react';
import { ThemeToggle } from '@/components/themeToggle.tsx';

const Header: FC = () => {
  return (
    <div className="flex h-12 items-center">
      <div className="flex flex-1 items-center justify-end">
        <ThemeToggle />
      </div>
    </div>
  );
};

export default Header;
